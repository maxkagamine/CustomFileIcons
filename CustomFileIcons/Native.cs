using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace CustomFileIcons
{
    static class Native
    {
        /// <summary>
        /// Shows the Default Programs control panel page for an app on pre-Win10 or the Default Apps settings page on Win10.
        /// </summary>
        /// <param name="appId">The app id registered under HKCU\Software\RegisteredApplications to show, where supported.</param>
        public static void ShowDefaultApps(string appId)
        {
            if (Environment.OSVersion.Version.Major < 10)
            {
                // Requires that the assembly company be set (https://stackoverflow.com/a/30642872)
                new ApplicationAssociationRegistrationUI().LaunchAdvancedAssociationUI(appId);
            }
            else
            {
                // As done by Chrome & Firefox; apparently not possible to directly open an app's settings
                // See https://stackoverflow.com/q/32178986
                new ApplicationActivationManager().ActivateApplication("windows.immersivecontrolpanel_cw5n1h2txyewy!microsoft.windows.immersivecontrolpanel", "page=SettingsPageAppsDefaults", ActivateOptions.None, out _);
            }
        }

        /// <summary>
        /// Gets the path of the executable associated with a file extension.
        /// </summary>
        /// <param name="extension">Extension without leading dot.</param>
        public static string GetAssociatedProgram(string extension)
        {
            uint length = 0;

            if (AssocQueryString(AssocF.None, AssocStr.Executable, "." + extension, null, null, ref length) != S_FALSE)
                return null;

            var str = new StringBuilder((int) length);

            if (AssocQueryString(AssocF.None, AssocStr.Executable, "." + extension, null, str, ref length) != S_OK)
                return null;

            return str.ToString();
        }

        #region IApplicationAssociationRegistrationUI::LaunchAdvancedAssociationUI

        // https://msdn.microsoft.com/en-us/library/windows/desktop/bb776330(v=vs.85).aspx
        // https://stackoverflow.com/q/28393592

        [ClassInterface(ClassInterfaceType.None)]
        [ComImport]
        [Guid("1968106d-f3b5-44cf-890e-116fcb9ecef1")]
        [TypeLibType(TypeLibTypeFlags.FCanCreate)]
        sealed class ApplicationAssociationRegistrationUI : IApplicationAssociationRegistrationUI
        {
            [MethodImpl(MethodImplOptions.InternalCall)]
            public extern void LaunchAdvancedAssociationUI(string appRegistryName);
        }

        [CoClass(typeof(ApplicationAssociationRegistrationUI))]
        [ComImport]
        [Guid("1f76a169-f994-40ac-8fc8-0959e8874710")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [TypeLibImportClass(typeof(ApplicationAssociationRegistrationUI))]
        interface IApplicationAssociationRegistrationUI
        {
            void LaunchAdvancedAssociationUI([MarshalAs(UnmanagedType.LPWStr)] string appRegistryName);
        }

        #endregion

        #region IApplicationActivationManager::ActivateApplication

        // https://msdn.microsoft.com/en-us/library/windows/desktop/hh706903.aspx
        // https://stackoverflow.com/a/12927313

        enum ActivateOptions
        {
            None = 0x00000000,  // No flags set
            DesignMode = 0x00000001,  // The application is being activated for design mode, and thus will not be able to
                                      // to create an immersive window. Window creation must be done by design tools which
                                      // load the necessary components by communicating with a designer-specified service on
                                      // the site chain established on the activation manager.  The splash screen normally
                                      // shown when an application is activated will also not appear.  Most activations
                                      // will not use this flag.
            NoErrorUI = 0x00000002,  // Do not show an error dialog if the app fails to activate.                                
            NoSplashScreen = 0x00000004,  // Do not show the splash screen when activating the app.
        }

        [ComImport, Guid("2e941141-7f97-4756-ba1d-9decde894a3d"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        interface IApplicationActivationManager
        {
            // Activates the specified immersive application for the "Launch" contract, passing the provided arguments
            // string into the application.  Callers can obtain the process Id of the application instance fulfilling this contract.
            IntPtr ActivateApplication([In] String appUserModelId, [In] String arguments, [In] ActivateOptions options, [Out] out UInt32 processId);
            IntPtr ActivateForFile([In] String appUserModelId, [In] IntPtr /*IShellItemArray* */ itemArray, [In] String verb, [Out] out UInt32 processId);
            IntPtr ActivateForProtocol([In] String appUserModelId, [In] IntPtr /* IShellItemArray* */itemArray, [Out] out UInt32 processId);
        }

        [ComImport, Guid("45BA127D-10A8-46EA-8AB7-56EA9078943C")]//Application Activation Manager
        class ApplicationActivationManager : IApplicationActivationManager
        {
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)/*, PreserveSig*/]
            public extern IntPtr ActivateApplication([In] String appUserModelId, [In] String arguments, [In] ActivateOptions options, [Out] out UInt32 processId);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            public extern IntPtr ActivateForFile([In] String appUserModelId, [In] IntPtr /*IShellItemArray* */ itemArray, [In] String verb, [Out] out UInt32 processId);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            public extern IntPtr ActivateForProtocol([In] String appUserModelId, [In] IntPtr /* IShellItemArray* */itemArray, [Out] out UInt32 processId);
        }

        #endregion

        #region AssocQueryString

        // http://msdn.microsoft.com/en-us/library/bb773471.aspx
        // https://stackoverflow.com/a/17773402

        [DllImport("Shlwapi.dll", CharSet = CharSet.Unicode)]
        static extern uint AssocQueryString(AssocF flags, AssocStr str, string pszAssoc, string pszExtra, [Out] StringBuilder pszOut, ref uint pcchOut);

        const int S_OK = 0;
        const int S_FALSE = 1;

        [Flags]
        enum AssocF
        {
            None = 0,
            Init_NoRemapCLSID = 0x1,
            Init_ByExeName = 0x2,
            Open_ByExeName = 0x2,
            Init_DefaultToStar = 0x4,
            Init_DefaultToFolder = 0x8,
            NoUserSettings = 0x10,
            NoTruncate = 0x20,
            Verify = 0x40,
            RemapRunDll = 0x80,
            NoFixUps = 0x100,
            IgnoreBaseClass = 0x200,
            Init_IgnoreUnknown = 0x400,
            Init_Fixed_ProgId = 0x800,
            Is_Protocol = 0x1000,
            Init_For_File = 0x2000
        }

        enum AssocStr
        {
            Command = 1,
            Executable,
            FriendlyDocName,
            FriendlyAppName,
            NoOpen,
            ShellNewValue,
            DDECommand,
            DDEIfExec,
            DDEApplication,
            DDETopic,
            InfoTip,
            QuickTip,
            TileInfo,
            ContentType,
            DefaultIcon,
            ShellExtension,
            DropTarget,
            DelegateExecute,
            Supported_Uri_Protocols,
            ProgID,
            AppID,
            AppPublisher,
            AppIconReference,
            Max
        }

        #endregion
    }

}
