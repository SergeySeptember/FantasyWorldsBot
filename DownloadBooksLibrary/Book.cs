namespace DownloadBooksLibrary
{
    public class Book
    {
        public string? Title { get; set; }
        public string? Author { get; set; }
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public string? ImagePath { get; set; }
        public string? DownloadLink { get; set; }
        public Dictionary<string, string> Formats { get; set; } = new();
    }
}