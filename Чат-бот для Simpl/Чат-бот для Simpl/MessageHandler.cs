using System;
using System.Collections.Generic;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace Чат_бот_для_Simpl
{
    class MessageHandler
    {
        const string ButtonFAQ = "FAQ";
        const string ButtonHR = "Связаться с HR";
        const string ButtonMood = "Мое настроение o(>ω<)o";

        private long botOwnerID = 1607927336; // временное айди HR

        // словарь для отслеживания состояния пользователей
        private Dictionary<long, string> userStates = new Dictionary<long, string>();

        public async void OnMessage(ITelegramBotClient client, Update update)
        {
            try
            {
                if (update.Message?.Text == "/start")
                {
                    await client.SendTextMessageAsync(update.Message?.Chat.Id ?? botOwnerID, "Приветствую!", replyMarkup: MyButtonsy());
                }
                else if (update.Message?.Text == "/help")
                {
                    await client.SendTextMessageAsync(update.Message?.Chat.Id ?? botOwnerID, "Мои команды\n/start\n/help");
                }
                else if (update.Message?.Text == ButtonFAQ)
                {
                    await client.SendTextMessageAsync(update.Message?.Chat.Id ?? botOwnerID, "ну хз...");
                    // do something
                }
                else if (update.Message?.Text == ButtonHR)
                {
                    await client.SendTextMessageAsync(update.Message?.Chat.Id ?? botOwnerID, "Пожалуйста, введите ваш вопрос.");
                    // состояние пользователя в ожидании вопроса
                    userStates[update.Message.Chat.Id] = "awaiting_hr_question";
                }
                else if (update.Message?.Text == ButtonMood)
                {
                    await client.SendTextMessageAsync(update.Message?.Chat.Id ?? botOwnerID, "(⌒‿⌒)");
                    // do something
                }
                else
                {
                    if (userStates.ContainsKey(update.Message.Chat.Id) && userStates[update.Message.Chat.Id] == "awaiting_hr_question")
                    {
                        // сброс состояния пользователя
                        userStates.Remove(update.Message.Chat.Id);

                        // перессылка вопроса HR (пока себе)
                        string userQuestion = update.Message.Text;
                        string userName = update.Message.From.Username != null ? $"@{update.Message.From.Username}" : update.Message.From.FirstName;

                        await client.SendTextMessageAsync(botOwnerID, $"Вопрос от {userName}:\n{userQuestion}");
                        await client.SendTextMessageAsync(update.Message.Chat.Id, "Ок, HR с Вами свяжется, ожидайте.");
                    }
                    else
                    {
                        await client.SendTextMessageAsync(update.Message?.Chat.Id ?? botOwnerID, update.Message?.Text ?? "[не текст]");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка: {ex.Message}");
                await client.SendTextMessageAsync(update.Message?.Chat.Id ?? botOwnerID, "Произошла ошибка, попробуйте снова позже.");
            }
        }

        // кнопочки в клавиатуре
        private IReplyMarkup MyButtonsy()
        {
            return new ReplyKeyboardMarkup(
                new List<List<KeyboardButton>>
                {
                    new List<KeyboardButton> // первая строка кнопочек
                    {
                        new KeyboardButton(ButtonFAQ),
                        new KeyboardButton(ButtonHR)
                    },
                    new List<KeyboardButton> // вторая строка кнопочек
                    {
                        new KeyboardButton(ButtonMood)
                    }
                }
            )
            {
                ResizeKeyboard = true
            };
        }

        // Кнопочки прямо под стартовым сообщением
        // если брать такой вариант, то каждая кнопка будет посылать callbackData 
        // ибо создать inline кнопку только с текстовым полем нельзя
        //
        // private IReplyMarkup MyButtonsy()
        // {          
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
