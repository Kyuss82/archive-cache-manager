using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace ArchiveCacheManager
{
    public static class NusFetcher
    {
        private const string NusBaseUrl = "http://ccs.shop.wii.com/ccs/download";
        private const string CtrNusBaseUrl = "http://nus.cdn.c.shop.nintendowifi.net/ccs/download";
        private const string TwlNusBaseUrl = "http://nus.cdn.shop.wii.com/ccs/download";

        private static readonly HttpClient HttpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(30)
        };

        public static bool DownloadTicket(string titleIdHex, string destinationPath)
        {
            string url = string.Format("{0}/{1}/cetk", NusBaseUrl, titleIdHex);
            return Download(url, destinationPath);
        }

        public static bool DownloadCtrTicket(string titleIdHex, string destinationPath)
        {
            string url = string.Format("{0}/{1}/cetk", CtrNusBaseUrl, titleIdHex);
            return Download(url, destinationPath);
        }

        public static bool DownloadTwlTicket(string titleIdHex, string destinationPath)
        {
            string url = string.Format("{0}/{1}/cetk", TwlNusBaseUrl, titleIdHex);
            return Download(url, destinationPath);
        }

        private static bool Download(string url, string destinationPath)
        {
            try
            {
                Logger.Log(string.Format("NUS download: {0} -> {1}", url, destinationPath));

                Task<HttpResponseMessage> getTask = HttpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
                getTask.Wait();

                using (HttpResponseMessage response = getTask.Result)
                {
                    if (!response.IsSuccessStatusCode)
                    {
                        Logger.Log(string.Format("NUS request failed: HTTP {0} for {1}", (int)response.StatusCode, url));
                        return false;
                    }

                    using (var fs = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        Task copyTask = response.Content.CopyToAsync(fs);
                        copyTask.Wait();
                    }
                }

                return new FileInfo(destinationPath).Length > 0;
            }
            catch (Exception ex)
            {
                Logger.Log(string.Format("NUS download error: {0}\r\n{1}", url, ex.ToString()), Logger.LogLevel.Exception);
                return false;
            }
        }
    }
}
