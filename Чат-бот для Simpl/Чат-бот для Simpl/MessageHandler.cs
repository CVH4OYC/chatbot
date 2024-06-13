using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot;

namespace Чат_бот_для_Simpl
{
    class MessageHandler
    {
        private long botOwnerID = 5841322604; // HINT: пока что захардкодил тут свой тг, потом можно через конфиг считывать

        public async void OnMessage(ITelegramBotClient client, Update update)
        {
            if (update.Message?.Text == "/start")
            {
                await client.SendTextMessageAsync(update.Message?.Chat.Id ?? botOwnerID, "Добро пожаловать!");
            }
            else if (update.Message?.Text == "/help")
            {
                await client.SendTextMessageAsync(update.Message?.Chat.Id ?? botOwnerID, "Мои команды\n/start\n/help");
            }
            else
            {
                await client.SendTextMessageAsync(update.Message?.Chat.Id ?? botOwnerID, update.Message?.Text ?? "[не текст]");
            }
        }
    }
}
