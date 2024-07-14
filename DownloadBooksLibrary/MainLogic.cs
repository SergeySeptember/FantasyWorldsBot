namespace DownloadBooksLibrary
{
    public class MainLogic
    {
        private readonly string _saveDirectory = @$"{Environment.CurrentDirectory}\Files\";
        private readonly DownloadData _downloadData;

        public MainLogic(string ip, int port)
        {
            //_downloadData = new(ip, port);
            _downloadData = new("51.222.245.101", 80);

            // Проверка на наличие папки для сохранения файлов
            if (!Directory.Exists(_saveDirectory))
                Directory.CreateDirectory(_saveDirectory);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        public void UpdateProxyAddress(string ip, int port) => _downloadData.UpdateProxyAddress(ip, port);

        public void ClearFileDirectory()
        {
            return;
        }

        public async Task<List<Book>> FoundBooks(string query)
        {
            string responseBody = await _downloadData.DownloadXMLPage(query);
            List<Book> booksList = PagesParser.ParseBooksFromHtml(responseBody);

            byte[]? imageBytes = [];
            foreach (Book item in booksList)
            {
                // Если ссылка на картинку отсутствует
                if (item.ImageUrl is null) break;

                // Пытаемся скачать картинки
                for (int i = 0; i < 10; i++)
                {
                    imageBytes = await _downloadData.DownloadImageAsync(item.ImageUrl);

                    if (imageBytes is not null)
                        break;
                }

                // Если за 10 попыток скачать так и не удалось
                if (imageBytes is null) continue;

                string imageName = Path.Combine(_saveDirectory, $"{item.Title}.jpg"); // Формируем путь к скаченной картинке
                item.ImagePath = imageName;
                await File.WriteAllBytesAsync(imageName, imageBytes); // Сохраняем картинку в папку
            }

            return booksList;
        }

        public async Task<string> GetBooksPath(Book book, string format)
        {
            byte[]? bookBytes = [];

            if (book.DownloadLink is null) // ToDo: переделать на цифры
                return "Отсутствует ссылка на файл";

            for (int i = 0; i < 10; i++)
            {
                bookBytes = await _downloadData.DownloadBookAsync(book.DownloadLink, format);

                if (bookBytes is not null)
                    break;
            }

            // Если за 10 попыток скачать так и не удалось
            if (bookBytes is null)
                return "Не удалось скачать книгу";

            string fileName = Path.Combine(_saveDirectory, $"{book.Title}.{format}");
            await File.WriteAllBytesAsync(fileName, bookBytes);

            return fileName;
        }
    }
}