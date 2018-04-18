using System;
using System.IO;
using System.Threading.Tasks;
using Crc32C;
using ImageMagick;
using PuppeteerSharp;

namespace CustomFileIcons.Generator
{
    class Program
    {
        private static readonly int[] IconSizes = new[] { 256, 128, 64, 48, 32, 24, 16 };

        static async Task<int> Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine($"Usage: {AppDomain.CurrentDomain.FriendlyName} [--hash] <svg path> <ico path>\n");
                Console.WriteLine($"Options:");
                Console.WriteLine($"  --hash    Append a CRC32C hash to the ico filename to prevent caching.");
                return 1;
            }

            bool appendHash = args[0] == "--hash";
            string svgPath = Path.GetFullPath(appendHash ? args[1] : args[0]);
            string icoPath = Path.GetFullPath(appendHash ? args[2] : args[1]);

            if (!File.Exists(svgPath))
            {
                Console.WriteLine($"Could not find svg '{svgPath}'.");
                return 1;
            }

            using (var images = new MagickImageCollection())
            {
                Console.WriteLine("Downloading Chromium");
                var downloader = new Downloader(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), ".local-chromium"));
                await downloader.DownloadRevisionAsync(Downloader.DefaultRevision);

                Console.WriteLine("Launching headless Chrome");
                var options = new LaunchOptions()
                {
                    Headless = true,
                    ExecutablePath = downloader.GetExecutablePath(Downloader.DefaultRevision)
                };
                using (var browser = await Puppeteer.LaunchAsync(options, Downloader.DefaultRevision))
                using (var page = await browser.NewPageAsync())
                {
                    Console.WriteLine("Opening svg");
                    await page.GoToAsync("file:///" + svgPath.Replace('\\', '/'));

                    foreach (int size in IconSizes)
                    {
                        Console.WriteLine($"Rendering {size}x{size} image");
                        await page.SetViewport(new ViewPortOptions() { Width = size, Height = size });

                        using (var stream = await page.ScreenshotStreamAsync(new ScreenshotOptions() { OmitBackground = true }))
                        {
                            images.Add(new MagickImage(stream));
                        }
                    }
                }

                Console.WriteLine("Generating ico");
                using (var stream = new MemoryStream())
                {
                    images.Write(stream, MagickFormat.Ico);
                    byte[] bytes = stream.ToArray();

                    if (appendHash)
                    {
                        string hash = Crc32CAlgorithm.Compute(bytes).ToString("x8");
                        icoPath = Path.ChangeExtension(icoPath, hash + Path.GetExtension(icoPath));
                    }

                    File.WriteAllBytes(icoPath, bytes);
                    Console.WriteLine($"Saved {Path.GetFileName(icoPath)}");
                }
            }

            return 0;
        }
    }
}
