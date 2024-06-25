using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Чат_бот_для_Simpl.Чат_бот_для_Simpl;

namespace Чат_бот_для_Simpl
{
    public class Host
    {
        private TelegramBotClient _bot;
        private Action<ITelegramBotClient, Update>? OnMessage; // делегат

        private Database _db;
        private Dictionary<string, string> _faq;


        public Host(string token, string dbConnectionString)
        {
            _bot = new TelegramBotClient(token);
            _db = new Database(dbConnectionString); // используем загрузчик из базы данных
            _faq = _db.LoadFAQ(); // загрузка FAQ из базы данных
        }

        public void Start()
        {
            _bot.StartReceiving(UpdateHandler, ErrorHandler);
            Console.WriteLine("Бот запущен");

            // подписка на получение сообщений от обработчика
            MessageHandler messageHandler = new MessageHandler(_faq, _db);
            OnMessage += messageHandler.OnMessage;
        }

        // это для обработки ошибок, если они будут возникать
        private async Task ErrorHandler(ITelegramBotClient client, Exception exception, CancellationToken token)
        {
            Console.WriteLine($"Ошибка: {exception.Message}");
            await Task.CompletedTask;
        }

        // это для приёма сообщений от бота
        private async Task UpdateHandler(ITelegramBotClient client, Update update, CancellationToken token)
        {
            Console.WriteLine($"Пришло сообщение: {update.Message?.Text ?? "[не текст]"}");
            OnMessage?.Invoke(client, update);
            await Task.CompletedTask;
        }
    }
}