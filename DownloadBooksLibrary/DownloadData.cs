using System.Net;

namespace DownloadBooksLibrary
{
    internal class DownloadData(string ip, int port)
    {
        private int _port = port;
        private string _ip = ip;

        /// <summary>
        /// Обновляет сетевой адрес и порт прокси сервера
        /// </summary>
        /// <param name="ip">Сетевой адрес сервера</param>
        /// <param name="port">Порт сервера</param>
        public void UpdateProxyAddress(string ip, int port)
        {
            _ip = ip;
            _port = port;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="titleName"></param>
        /// <returns></returns>
        public async Task<string> DownloadXMLPage(string titleName)
        {
            HttpClientHandler handler = new()
            {
                Proxy = new WebProxy(_ip, _port),
                UseProxy = true
            };

            using (HttpClient client = new(handler))
            {
                try
                {
                    HttpResponseMessage response = await client.GetAsync($"http://fantasy-worlds.org/search/?q={titleName}");
                    string responseBody = await response.Content.ReadAsStringAsync();
                    return responseBody;
                }
                catch (Exception)
                {
                    return string.Empty;
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="imagePath"></param>
        /// <returns></returns>
        public async Task<byte[]?> DownloadImageAsync(string imagePath)
        {
            HttpClientHandler handler = new()
            {
                Proxy = new WebProxy(_ip, _port),
                UseProxy = true
            };

            using (HttpClient client = new(handler))
            {
                try
                {
                    string downloadLink = $"http://fantasy-worlds.org{imagePath}";
                    byte[] imageBytes = await client.GetByteArrayAsync(downloadLink);
                    return imageBytes;
                }
                catch (Exception)
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="format"></param>
        /// <returns></returns>
        public async Task<byte[]?> DownloadBookAsync(string filePath, string format)
        {
            HttpClientHandler handler = new()
            {
                Proxy = new WebProxy(_ip, _port),
                UseProxy = true
            };

            using (HttpClient client = new(handler))
            {
                try
                {
                    client.Timeout = TimeSpan.FromMinutes(1);

                    string downloadLink = $"http://fantasy-worlds.org{filePath}{format}";
                    HttpResponseMessage response = await client.GetAsync(downloadLink, HttpCompletionOption.ResponseHeadersRead);

                    HttpResponseMessage temp = response.EnsureSuccessStatusCode();
                    if (!temp.IsSuccessStatusCode) // Если скачать файл не удалось
                        return null;

                    using (MemoryStream memoryStream = new())
                    using (Stream contentStream = await response.Content.ReadAsStreamAsync())
                    {
                        await contentStream.CopyToAsync(memoryStream);
                        return memoryStream.ToArray();
                    }
                }
                catch (Exception)
                {
                    return null;
                }
            }
        }
    }
}
