using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;

namespace FantasyWorldsBot
{
    internal class Program
    {
        public static async Task Main(string[] args)
        {
            string token = "";
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
                updateHandler: HandleUpdateAsync,               // Обработчик новых обновлений
                pollingErrorHandler: HandlePollingErrorAsync,   // Обработчик ошибок
                receiverOptions: receiverOptions,               // Параметры получения обновлений
                cancellationToken: cts.Token                    // Токен для отмены операции
            );






            // Асинхронный обработчик для каждого нового обновления
            async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
            {
                try
                {
                    if (update.Message is not { } message) return; // Если обновление не содержит сообщения

                    return;
                }
                catch (Exception)
                {
                    
                }
            }

            // Асинхронный обработчик ошибок
            async Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
            {

                    // Вывод сообщения об ошибке в консоль
                return;
            }
            Console.ReadKey();
        }
    }
}