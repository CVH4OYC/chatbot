using System;
using System.Collections.Generic;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace Чат_бот_для_Simpl
{
    class MessageHandler
    {
        const string ButtonFAQ = "FAQ";
        const string ButtonHR = "Связаться с HR";
        const string ButtonMood = "Мое настроение o(>ω<)o";

        private long botOwnerID = 746106815; // временное айди HR

        // словарь для отслеживания состояния пользователей
        private Dictionary<long, string> userStates = new Dictionary<long, string>();

        // словарь с вопросами и ответами 
        private Dictionary<string, string> _faq;
        private InlineKeyboardMarkup _faqInlineKeyboard; // Кэш inline клавиатуры FAQ
        public MessageHandler(Dictionary<string, string> faq)
        {
            _faq = faq;
            _faqInlineKeyboard = BuildInlineKeyboard();
        }
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
                    await ShowFAQ(client, update.Message.Chat.Id);
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
                else if (update.CallbackQuery != null) // Обработка callback-запросов
                {
                    await HandleCallbackQuery(client, update.CallbackQuery);
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

        // отображает inline клавиатуру с вопросами
        private async Task ShowFAQ(ITelegramBotClient client, long chatId)
        {
            try
            {
                await client.SendTextMessageAsync(chatId, "Выберите вопрос из FAQ:", replyMarkup: _faqInlineKeyboard);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при выводе FAQ: {ex.Message}");
                await client.SendTextMessageAsync(chatId, "Произошла ошибка при выводе FAQ.");
            }
        }
        // создаём inline клавиаутур из вопросов, хранящихся в словаре
        private InlineKeyboardMarkup BuildInlineKeyboard()
        {
            var inlineKeyboard = new List<List<InlineKeyboardButton>>();

            foreach (var question in _faq.Keys)
            {
                var row = new List<InlineKeyboardButton>
                {
                    InlineKeyboardButton.WithCallbackData(question)
                };
                inlineKeyboard.Add(row);
            }

            return new InlineKeyboardMarkup(inlineKeyboard);
        }
        //для обработки ответов с inline клавиатуры
        private async Task HandleCallbackQuery(ITelegramBotClient client, CallbackQuery callbackQuery)
        {
            try
            {
                string question = callbackQuery.Data;

                if (_faq.ContainsKey(question))
                {
                    string answer = _faq[question];
                    await client.SendTextMessageAsync(callbackQuery.Message.Chat.Id, $"Вопрос: {question}\nОтвет:\n {answer}", parseMode: ParseMode.MarkdownV2);
                }
                else
                {
                    await client.SendTextMessageAsync(callbackQuery.Message.Chat.Id, "Ответ на этот вопрос не найден.");
                }

                // Удаление инлайн клавиатуры после нажатия
                await client.DeleteMessageAsync(callbackQuery.Message.Chat.Id, callbackQuery.Message.MessageId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при обработке callback-запроса: {ex.Message}");
            }
        }
    }
}
