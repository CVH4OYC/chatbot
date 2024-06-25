using Npgsql;
using System;
using System.Collections.Generic;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Чат_бот_для_Simpl.Чат_бот_для_Simpl;
using FuzzySharp;

namespace Чат_бот_для_Simpl
{
    class MessageHandler
    {
        const string ButtonFAQ = "FAQ";
        const string ButtonHR = "Связаться с HR";
        const string ButtonMood = "Мое настроение o(>ω<)o";

        private long botOwnerID = 746106815; // временное айди HR

        // словарь для отслеживания состояния пользователей (для связи с HR и указания настроения)
        private Dictionary<long, string> userStates = new Dictionary<long, string>();

        // словарь с вопросами и ответами 
        private Dictionary<string, string> _faq;
        private InlineKeyboardMarkup _faqInlineKeyboard; // Кэш inline клавиатуры FAQ

        private Database _db; // через этот объект обращение к бд (в Database прописать нужные методы)
        public MessageHandler(Dictionary<string, string> faq, Database db)
        {
            _faq = faq;
            _db = db;
            _faqInlineKeyboard = BuildInlineKeyboard();
        }
        public async void OnMessage(ITelegramBotClient client, Update update)
        {
            try
            {
                if (update.Message?.Text == "/start")
                {
                    await client.SendTextMessageAsync(update.Message?.Chat.Id ?? botOwnerID, "Привет :)  Я рад, что ты присоединился к Simpl!", replyMarkup: MyButtonsy());
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
                    else // всё, что отправляется просто так считаем вопросами
                    {
                        string userQuestion = update.Message.Text;
                        string answer = FindAnswer(userQuestion);
                        await client.SendTextMessageAsync(update.Message?.Chat.Id ?? botOwnerID, answer, parseMode: ParseMode.MarkdownV2);
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

                    // Проверка и добавление записи в историю вопросов
                    string tgNickname = callbackQuery.From.Username;
                    int employeeId = _db.GetEmployeeId(tgNickname);
                    if (employeeId != -1)
                    {
                        int questionId = _db.GetQuestionId(question); // Получаем question_id по тексту вопроса
                        if (questionId != -1)
                        {
                            _db.AddQuestionHistoryRecord(employeeId, questionId);
                        }
                    }
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
        // Метод для получения ответа на вопрос
        private string FindAnswer(string userQuestion)
        {
            // Прямое совпадение
            if (_faq.TryGetValue(userQuestion, out string directAnswer))
            {
                return directAnswer;
            }

            // Нечеткий поиск с использованием FuzzySharp
            var fuzzyMatch = _faq.OrderByDescending(qa => Fuzz.Ratio(qa.Key, userQuestion)).FirstOrDefault();
            if (fuzzyMatch.Key != null && Fuzz.Ratio(fuzzyMatch.Key, userQuestion) > 60) // Пороговое значение 70
            {
                return fuzzyMatch.Value;
            }

            return "Извините, на такой вопрос пока нет ответа\\.";
        }
    }
}
