using DownloadBooksLibrary;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using System.IO;

namespace FantasyWorldsBot
{
    internal class Program
    {
        private static MainLogic mainLogic = new("141.11.40.11", 80); // :
        private static List<Book> books = new();
        private static readonly int PageSize = 1;
        private static Dictionary<long, List<int>> userMessages = new Dictionary<long, List<int>>();

        public static async Task Main(string[] args)
        {
            string token = "7473088130:AAEWItiIc5ZJeCz9RwkSVEPj5nfMJbVu_aI";
            if (string.IsNullOrEmpty(token))
            {
                await Console.Out.WriteLineAsync("Токена нет");
                return;
            }

            // Создаём бота с использованием токена
            TelegramBotClient botClient = new(token);
            using CancellationTokenSource cts = new();

            // Настройка параметров получения обновлений
            ReceiverOptions receiverOptions = new()
            {
                AllowedUpdates = Array.Empty<UpdateType>() // Получение всех типов обновлений, кроме связанных с изменениями членства в чате
            };

            // Запуск асинхронного приема обновлений
            botClient.StartReceiving(
                HandleUpdateAsync, // Обработчик новых обновлений
                HandlePollingErrorAsync, // Обработчик ошибок
                receiverOptions, // Параметры получения обновлений
                cts.Token // Токен для отмены операции
            );

            Console.WriteLine($"Bot started. Press any key to exit.");
            Console.ReadKey();

            cts.Cancel();


            static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
            {
                try
                {
                    if (update.Message is not null)
                    {
                        if (!userMessages.ContainsKey(update.Message.Chat.Id))
                        {
                            userMessages[update.Message.Chat.Id] = new List<int>();
                        }

                        userMessages[update.Message.Chat.Id].Add(update.Message.MessageId);

                        if (update.Message.Text is not null)
                            switch (update.Message.Text)
                            {
                                case "/Старт":
                                    ReplyKeyboardMarkup replyKeyboard = new ReplyKeyboardMarkup(new[]
                                    {
                                        new KeyboardButton[] { "/Старт", "/clear", "/Установить параметры прокси" }
                                    })
                                    {
                                        ResizeKeyboard = true
                                    };

                                    await botClient.SendTextMessageAsync(
                                        update.Message.Chat.Id,
                                        "Добро пожаловать в неофициальный бот fantasy-worlds.org! Скачивай любимые книги через удобный телеграм бот, если получится...",
                                        replyMarkup: replyKeyboard
                                    );
                                    return;

                                case "/clear":
                                    await ClearChatAsync(update.Message.Chat.Id);
                                    return;

                                case "/Установить параметры прокси":
                                    string ip = "192.168.1.1"; // Замени на фактический IP
                                    string port = "8080"; // Замени на фактический порт
                                    await botClient.SendTextMessageAsync(
                                        update.Message.Chat.Id,
                                        $"IP: {ip}\nPort: {port}"
                                    );
                                    return;
                            }

                        Console.WriteLine(update.Message.Text);
                        books = await mainLogic.FoundBooks(update.Message.Text);
                        if (books.Count == 0)
                        {
                            Console.WriteLine("Books is 0");
                            return;
                        }

                        await SendPageAsync(botClient, update.Message.Chat.Id, 0);
                    }
                    else if (update.CallbackQuery is not null) await HandleCallbackQueryAsync(botClient, update.CallbackQuery);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }

                async Task ClearChatAsync(long chatId)
                {
                    if (userMessages.ContainsKey(chatId))
                    {
                        foreach (var messageId in userMessages[chatId])
                        {
                            try
                            {
                                await botClient.DeleteMessageAsync(chatId, messageId);
                            }
                            catch (ApiRequestException ex)
                            {
                                Console.WriteLine($"Error deleting message {messageId}: {ex.Message}");
                            }
                        }

                        userMessages[chatId].Clear();
                    }

                    var confirmationMessage = await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "All messages have been cleared."
                    );

                    userMessages[chatId].Add(confirmationMessage.MessageId);
                }
            }


            static async Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken) => Console.WriteLine(exception.Message);

            Console.ReadKey();
        }

        private static async Task HandleCallbackQueryAsync(ITelegramBotClient botClient, CallbackQuery callbackQuery)
        {
            try
            {
                string[] data = callbackQuery.Data.Split('_');
                string action = data[0];
                int page = int.Parse(data[1]);

                if (action == "next")
                    await SendPageAsync(botClient, callbackQuery.Message.Chat.Id, page + 1, callbackQuery.Message.MessageId);
                else if (action == "prev")
                    await SendPageAsync(botClient, callbackQuery.Message.Chat.Id, page - 1, callbackQuery.Message.MessageId);
                else if (action == "download")
                    await SendDownloadLinksAsync(botClient, callbackQuery.Message.Chat.Id, page, callbackQuery.Message.MessageId);
                else if (action == "book")
                {
                    int bookIndex = int.Parse(data[2]);
                    Book selectedBook = books[bookIndex];
                    await botClient.EditMessageTextAsync(
                        callbackQuery.Message.Chat.Id,
                        callbackQuery.Message.MessageId,
                        $"You selected: {selectedBook.Title}\n{selectedBook.Description}\n{selectedBook.DownloadLink}"
                    );
                }
                else if (action == "format")
                {
                    int bookIndex = int.Parse(data[1]);
                    string format = data[2];
                    Book selectedBook = books[bookIndex];
                    string filePath = await mainLogic.GetBooksPath(selectedBook, format);
                    using (FileStream fileStream = new(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        InputFileStream inputFile = new(fileStream, Path.GetFileName(filePath));
                        await botClient.SendDocumentAsync(callbackQuery.Message.Chat.Id, inputFile, caption: Path.GetFileName(filePath));
                    }
                }
                await botClient.AnswerCallbackQueryAsync(callbackQuery.Id); // Подтверждаем получение callback-запроса
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }


        private static async Task SendDownloadLinksAsync(ITelegramBotClient botClient, long chatId, int bookIndex, int messageId)
        {
            Book book = books[bookIndex];
            List<List<InlineKeyboardButton>> inlineKeyboard = new();

            foreach (KeyValuePair<string, string> format in book.Formats)
            {
                InlineKeyboardButton button = InlineKeyboardButton.WithCallbackData(format.Key, $"format_{bookIndex}_{format.Value}");
                inlineKeyboard.Add(new() { button });
            }

            InlineKeyboardMarkup replyMarkup = new(inlineKeyboard);

            await botClient.EditMessageReplyMarkupAsync(chatId, messageId, replyMarkup);
        }


        private static async Task SendPageAsync(ITelegramBotClient botClient, long chatId, int page, int? messageId = null)
        {
            int startIndex = page * PageSize;
            int endIndex = Math.Min(startIndex + PageSize, books.Count);
            List<Book> pageBooks = books.Skip(startIndex).Take(PageSize).ToList();

            if (pageBooks.Count == 0) return;

            Book book = pageBooks.First();

            // Загружаем изображение в память
            byte[] imageData;
            using (FileStream stream = new(book.ImagePath, FileMode.Open, FileAccess.Read))
            {
                using (MemoryStream memoryStream = new())
                {
                    await stream.CopyToAsync(memoryStream);
                    imageData = memoryStream.ToArray();
                }
            }

            // Создаем объект для отправки изображения
            using (MemoryStream memoryStream = new(imageData))
            {
                InputFileStream photo = new(memoryStream, "book_image");

                string caption = $"*{book.Title}*\n{book.Author}\n{book.Description})";

                List<InlineKeyboardButton> navigationButtons = new();
                if (page > 0)
                    navigationButtons.Add(InlineKeyboardButton.WithCallbackData("<", $"prev_{page}"));
                navigationButtons.Add(InlineKeyboardButton.WithCallbackData("Download", $"download_{page}"));
                if (endIndex < books.Count)
                    navigationButtons.Add(InlineKeyboardButton.WithCallbackData(">", $"next_{page}"));

                InlineKeyboardMarkup replyMarkup = new(navigationButtons);

                if (messageId == null)
                    await botClient.SendPhotoAsync(chatId, photo, caption: caption, parseMode: ParseMode.Markdown, replyMarkup: replyMarkup);
                else
                {
                    await botClient.EditMessageMediaAsync(
                        chatId,
                        messageId.Value,
                        new InputMediaPhoto(photo) { Caption = caption, ParseMode = ParseMode.Markdown }
                    );
                    await botClient.EditMessageCaptionAsync(
                        chatId,
                        messageId.Value,
                        caption,
                        ParseMode.Markdown,
                        replyMarkup: replyMarkup
                    );
                }
            }
        }
    }
}