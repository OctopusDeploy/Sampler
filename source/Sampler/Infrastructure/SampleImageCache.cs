using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using Humanizer;
using Polly;
using Polly.Retry;
using Serilog;

namespace Octopus.Sampler.Infrastructure
{
    public class SampleImageCache
    {
        private static readonly ILogger Log = Serilog.Log.ForContext(typeof (SampleImageCache));

        private static readonly RetryPolicy Retry =
            Policy.Handle<Exception>()
                .WaitAndRetry(2, attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
                    (exception, span) => Log.Warning(exception, "{Message} Trying again in {Wait}...", exception.Message, span.Humanize()));

        static bool isRoboHashDown = false;

        public static string DownloadImage(string downloadUrl, string useFileName = null)
        {
            var fileName = useFileName ?? GetSHA1HashString(downloadUrl);

            var imageCachePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Octopus\\Temp\\Samples\\ImageCache");
            if (!Directory.Exists(imageCachePath)) Directory.CreateDirectory(imageCachePath);
            var imageFilePath = Path.Combine(imageCachePath, fileName);
            if (File.Exists(imageFilePath))
            {
                Log.Debug("Found {URL} cached as {FileName}!", downloadUrl, imageFilePath);
            }
            else
            {
                Log.Information("Downloading {URL} and saving as {FileName}...", downloadUrl, imageFilePath);
                using (var client = new WebClient())
                {
                    Retry.Execute(() =>
                    {
                        // ReSharper disable once AccessToDisposedClosure
                        client.DownloadFile(downloadUrl, imageFilePath);
                    });
                }
            }

            return imageFilePath;
        }

        private static string GetSHA1HashString(string downloadUrl)
        {
            return SHA1.Create().ComputeHash(Encoding.UTF8.GetBytes(downloadUrl))
                .Aggregate(new StringBuilder(), (sb, b) => sb.AppendFormat("{0:x2}", b), sb => sb.ToString());
        }

        public static string GetRobotImage(string generatorSeed)
        {
            if (isRoboHashDown)
            {
                Log.Information("RoboHash appears to be down, skipping image.");

                return string.Empty;
            }
            try
            {
                return DownloadImage($"https://robohash.org/{generatorSeed}", $"{generatorSeed}.png");
            }
            catch (Exception)
            {
                isRoboHashDown = true;
                return string.Empty;
            }
        }
    }
}