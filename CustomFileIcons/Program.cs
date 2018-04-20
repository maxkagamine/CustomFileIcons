using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using CustomFileIcons.Proxy;
using Microsoft.Win32;
using Newtonsoft.Json.Linq;

namespace CustomFileIcons
{
    // Documentation on registering default apps:
    // https://msdn.microsoft.com/en-us/library/cc144154%28v=vs.85%29.aspx

    class Program
    {
        const string RegistryName = "CustomFileIcons";
        static readonly string ExePath = new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath;
        static readonly string ProxyExePath = new Uri(typeof(Proxy.Program).Assembly.CodeBase).LocalPath;
        static readonly string BaseDirectory = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(ExePath), @"..\..\..")); // TODO: Pass base dir as argument?
        static readonly string ProductName = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).ProductName;

        static void Main(string[] args)
        {
            // Read config file

            List<FileType> types = new List<FileType>();

            string json = File.ReadAllText(Path.Combine(BaseDirectory, "config.json"));
            JObject config = JObject.Parse(json);

            foreach (JProperty prop in config.Value<JObject>("types").Properties())
            {
                FileType type = prop.Value.ToObject<FileType>();
                type.Extension = prop.Name;
                type.Icon = ResolveIcon(type.Icon ?? type.Extension);
                type.Open = ResolveCommand(config, type.Open);
                type.Menu = type.Menu.ToDictionary(x => x.Key, x => ResolveCommand(config, x.Value));

                types.Add(type);
            }

            // Open registry

            // HKEY_CLASSES_ROOT by default writes to HKEY_LOCAL_MACHINE\Software\Classes which requires admin;
            // using HKEY_CURRENT_USER's instead to avoid requiring elevation and also to keep changes user-specific

            using (var currentUserKey = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64)) // Prevent System32 being rewritten to SysWow64
            using (var softwareKey = currentUserKey.OpenSubKey("Software", true))
            using (var classesKey = softwareKey.OpenSubKey("Classes", true))
            {
                // Nuke existing file types

                foreach (string keyName in classesKey.GetSubKeyNames())
                {
                    if (keyName.StartsWith($"{RegistryName}."))
                    {
                        classesKey.DeleteSubKeyTree(keyName);
                    }
                }

                // Add new file types

                foreach (FileType type in types)
                {
                    using (var key = classesKey.CreateSubKey(GetTypeId(type)))
                    {
                        key.SetValue(null, type.Name);

                        using (var iconKey = key.CreateSubKey("DefaultIcon"))
                        {
                            iconKey.SetValue(null, type.Icon);
                        }

                        using (var shellKey = key.CreateSubKey("shell"))
                        {
                            using (var openKey = shellKey.CreateSubKey("open"))
                            using (var openCommandKey = openKey.CreateSubKey("command"))
                            {
                                openCommandKey.SetValue(null, $"{QuoteArguments.Quote(new[] { ProxyExePath })} {type.Open}"); // Open through proxy in order to appear as one app
                            }

                            foreach (var menuItem in type.Menu)
                            {
                                string menuItemId = Regex.Replace(menuItem.Key, "&(?!&)", "");

                                using (var menuItemKey = shellKey.CreateSubKey(menuItemId))
                                using (var menuItemCommandKey = menuItemKey.CreateSubKey("command"))
                                {
                                    menuItemKey.SetValue(null, menuItem.Key);
                                    menuItemCommandKey.SetValue(null, menuItem.Value);
                                }
                            }
                        }
                    }
                }

                // Register as an option in Default Apps

                using (var registeredAppsKey = softwareKey.OpenSubKey("RegisteredApplications", true))
                using (var appKey = softwareKey.CreateSubKey(RegistryName))
                using (var capabilitiesKey = appKey.CreateSubKey("Capabilities"))
                {
                    capabilitiesKey.SetValue("ApplicationDescription", ProductName);
                    capabilitiesKey.DeleteSubKeyTree("FileAssociations", false);

                    using (var fileAssocKey = capabilitiesKey.CreateSubKey("FileAssociations"))
                    {
                        foreach (FileType type in types)
                        {
                            fileAssocKey.SetValue($".{type.Extension}", GetTypeId(type));

                            foreach (string alias in type.Aliases)
                            {
                                fileAssocKey.SetValue($".{alias}", GetTypeId(type));
                            }
                        }
                    }

                    registeredAppsKey.SetValue(RegistryName, $@"Software\{RegistryName}\Capabilities");
                }
            }

            Native.UpdateShellAssociations();

            Console.WriteLine("Registered file types.");

            // Create test files

            var extensions = types.SelectMany(t => new[] { t.Extension }.Concat(t.Aliases)).OrderBy(x => x).ToList();

            string testDir = Path.Combine(BaseDirectory, "test");
            Directory.CreateDirectory(testDir);

            foreach (string file in Directory.EnumerateFiles(testDir, "test.*"))
            {
                if (!extensions.Contains(Path.GetExtension(file).Substring(1)))
                {
                    File.Delete(file);
                }
            }

            foreach (string ext in extensions)
            {
                File.Create(Path.Combine(testDir, "test." + ext));
            }

            // Show settings UI if any not already set

            Func<string[]> getUnset = () => extensions.Where(x => !Native.GetAssociatedProgram(x).Equals(ProxyExePath, StringComparison.OrdinalIgnoreCase)).ToArray();
            var unset = getUnset();

            if (unset.Length > 0)
            {
                if (unset.Length < extensions.Count)
                {
                    Console.WriteLine("\nThe following extensions are not currently associated with the custom types:");
                    foreach (string ext in unset)
                    {
                        Console.WriteLine("  " + ext);
                    }
                    Console.WriteLine();
                }

                if (Environment.OSVersion.Version.Major < 10)
                {
                    Console.WriteLine("Opening Default Programs. Select all and save.");
                }
                else
                {
                    Console.WriteLine($"Opening Default Apps. Choose \"Set defaults by app\" and find \"{ProductName}\".");
                }

                Native.ShowDefaultApps(RegistryName);

                // Wait until all associations are set

                int unsetCount = unset.Length;
                bool hasPrinted = false;

                while (unsetCount > 0)
                {
                    Thread.Sleep(500);

                    unset = getUnset();
                    if (unset.Length != unsetCount)
                    {
                        unsetCount = unset.Length;

                        if (hasPrinted)
                        {
                            Console.CursorTop--;
                            Console.Write(new string(' ', Console.BufferWidth));
                            Console.CursorTop--;
                        }

                        if (unsetCount > 1)
                        {
                            Console.WriteLine($"{unsetCount} left.");
                        }
                        else if (unsetCount == 1)
                        {
                            Console.WriteLine("Last one.");
                        }
                        
                        hasPrinted = true;
                    }
                }
            }
            else
            {
                Console.WriteLine("All extensions are already associated with the custom types.");
            }

            // Whoop

            Console.WriteLine("Done.");
        }

        static string ResolveIcon(string icon)
        {
            if (icon.Contains("\\"))
                return icon;

            return Directory.EnumerateFiles(Path.Combine(BaseDirectory, "icons"), $"{icon}.*").FirstOrDefault() ??
                throw new Exception($"No icon named '{icon}'.");
        }

        static string ResolveCommand(JObject config, string command)
        {
            return config["commands"].Value<string>(command ?? "default") ?? command;
        }

        static string GetTypeId(FileType type) => $"{RegistryName}.{type.Extension}";
    }
}
