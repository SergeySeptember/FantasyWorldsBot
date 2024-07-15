using HtmlAgilityPack;

namespace DownloadBooksLibrary
{
    internal class PagesParser
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="page">Скачанная html страница</param>
        /// <returns>Список найденных и распаршенных книг</returns>
        public static List<Book> ParseBooksFromHtml(string page)
        {
            List<Book> books = new();

            // Если страницу получить не удалось
            if (string.IsNullOrEmpty(page)) return books;

            HtmlDocument doc = new();
            doc.LoadHtml(page);

            // Перебираем ноды и парсим данные о найденных книгах
            foreach (HtmlNode? bookNode in doc.DocumentNode.SelectNodes("//div[@itemscope='itemscope' and @itemtype='http://schema.org/Book']"))
            {
                Book book = new();

                HtmlNode? titleNode = bookNode.SelectSingleNode(".//span[@itemprop='name']");
                if (titleNode != null)
                    book.Title = titleNode.InnerText.Trim();

                HtmlNode? authorNode = bookNode.SelectSingleNode(".//a[@itemprop='author']");
                if (authorNode != null)
                    book.Author = authorNode.InnerText.Trim();

                HtmlNode? descriptionNode = bookNode.SelectSingleNode(".//span[@itemprop='description']");
                if (descriptionNode != null)
                    book.Description = descriptionNode.InnerText.Trim();

                HtmlNode? imageNode = bookNode.SelectSingleNode(".//img[@itemprop='image']");
                if (imageNode != null)
                    book.ImageUrl = imageNode.GetAttributeValue("src", "").Trim();

                HtmlNode? downloadLinkNode = bookNode.SelectSingleNode(".//a[@id and contains(@id, 'download_book')]");
                if (downloadLinkNode != null)
                    book.DownloadLink = downloadLinkNode.GetAttributeValue("href", "").Trim();

                HtmlNodeCollection? formatNodes = bookNode.SelectNodes(".//select[@class='_nb-select-fallback']/option");
                if (formatNodes != null)
                    foreach (HtmlNode? formatNode in formatNodes)
                    {
                        string format = formatNode.InnerText.Trim();
                        string value = formatNode.GetAttributeValue("value", "").Trim();
                        book.Formats.Add(format, value);
                    }

                books.Add(book);
            }

            return books;
        }
    }
}