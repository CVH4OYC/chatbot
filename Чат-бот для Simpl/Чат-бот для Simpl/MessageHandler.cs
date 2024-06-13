using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;

namespace Чат_бот_для_Simpl
{
    class MessageHandler
    {
        const string ButtonFAQ = "FAQ";
        const string ButtonHR = "Связаться с HR";
        const string ButtonMood = "Мое настроение o(>ω<)o";

        private long botOwnerID = 5841322604; // HINT: пока что захардкодил тут свой тг, потом можно через конфиг считывать

        public async void OnMessage(ITelegramBotClient client, Update update)
        {
            if (update.Message?.Text == "/start")
            {
                await client.SendTextMessageAsync(update.Message?.Chat.Id ?? botOwnerID, "Добро пожаловать!", replyToMessageId: update.Message?.MessageId,
                 replyMarkup: MyButtonsy());
            }
            else if (update.Message?.Text == "/help")
            {
                await client.SendTextMessageAsync(update.Message?.Chat.Id ?? botOwnerID, "Мои команды\n/start\n/help", replyToMessageId: update.Message?.MessageId);
            }
            else if (update.Message?.Text == ButtonFAQ)
            {
                await client.SendTextMessageAsync(update.Message?.Chat.Id ?? botOwnerID, "ну хз...", replyToMessageId: update.Message?.MessageId);
                // do something
            }
            else if (update.Message?.Text == ButtonHR)
            {
                await client.SendTextMessageAsync(update.Message?.Chat.Id ?? botOwnerID, "все ушли на обед", replyToMessageId: update.Message?.MessageId);
                // do something
            }
            else if (update.Message?.Text == ButtonMood)
            {
                await client.SendTextMessageAsync(update.Message?.Chat.Id ?? botOwnerID, "(⌒‿⌒)", replyToMessageId: update.Message?.MessageId);
                // do something
            }
            else
            {
                await client.SendTextMessageAsync(update.Message?.Chat.Id ?? botOwnerID, update.Message?.Text ?? "[не текст]", replyToMessageId: update.Message?.MessageId);
            }
        }

        // кнопочки в клавиатуре
        // update.Message?.Text  - название кнопки
        private IReplyMarkup MyButtonsy(){   
            return new ReplyKeyboardMarkup(
                new List<List<KeyboardButton>>{
                    new List<KeyboardButton> {  // первая строка кнопочек
                        new KeyboardButton(ButtonFAQ),
                        new KeyboardButton(ButtonHR)
                    },
                    new List<KeyboardButton> {  // вторая строка кнопочек
                        new KeyboardButton(ButtonMood)
                    }
                }
            );
        } 

        // Кнопочки прямо под стартовым сообщением
        // если брать такой вариант, то каждая кнопка будет посылать callbackData 
        // ибо создать inline кнопку только с текстовым полем нельзя
        //
        // private IReplyMarkup MyButtonsy(){          
        //     return new InlineKeyboardMarkup(
        //         new []{
        //             new []{
        //                 InlineKeyboardButton.WithCallbackData(ButtonFAQ, "нажали_кн1"),
        //                 InlineKeyboardButton.WithCallbackData(ButtonHR, "нажали_кн2")
        //             },
        //             new[]{
        //                 InlineKeyboardButton.WithCallbackData(ButtonMood, "нажали_кн3")
        //             }
        //         }
        //     );
        // } 
    }
}
