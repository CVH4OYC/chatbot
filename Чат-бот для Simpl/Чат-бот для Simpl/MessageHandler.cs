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

        const string ButtonSetMood = "Указать сегодняшнее настроение";
        const string ButtonGetMoodHistory = "Узнать настроение за последние 5 дней";

        private long botOwnerID; //айди HR

        // словарь для отслеживания состояния пользователей (для связи с HR и указания настроения)
        private Dictionary<long, string> userStates = new Dictionary<long, string>();

        // словарь с вопросами и ответами 
        private Dictionary<string, string> _faq;
        
        private InlineKeyboardMarkup _faqInlineKeyboard; // Кэш inline клавиатуры FAQ

        private Database _db; // через этот объект обращение к бд (в Database прописать нужные методы)

        private Dictionary<int, string> _moods;

        public MessageHandler(Dictionary<string, string> faq, Database db,string HRid)
        {
            _faq = faq;
            _db = db;
            _faqInlineKeyboard = BuildInlineKeyboard();
            _moods = _db.LoadMoods();
            // Преобразование строки HRid в тип long
            if (long.TryParse(HRid, out long result))
            {
                botOwnerID = result;
            }
            else
            {
                // Обработка случая, когда строка не может быть преобразована в long
                throw new ArgumentException("Неверный формат строки для HRid.");
            }
        }

        public async void OnMessage(ITelegramBotClient client, Update update)
        {
            try
            {
                if (update.Message != null)
                {
                    var message = update.Message;

                    if (message.Text == "/start")
                    {
                        await client.SendTextMessageAsync(message.Chat.Id, "Привет :)  Я рад, что ты присоединился к Simpl!", replyMarkup: MyButtonsy());
                    }
                    else if (message.Text == "/help")
                    {
                        await client.SendTextMessageAsync(message.Chat.Id, "Мои команды\n/start\n/help");
                    }
                    else if (message.Text == ButtonFAQ)
                    {
                        await ShowFAQ(client, message.Chat.Id);
                    }
                    else if (message.Text == ButtonHR)
                    {
                        await client.SendTextMessageAsync(message.Chat.Id, "Пожалуйста, введите сообщение для HR.");
                        userStates[message.Chat.Id] = "awaiting_hr_question";
                    }
                    else if (message.Text == ButtonMood)
                    {
                        await client.SendTextMessageAsync(message.Chat.Id, "Выберите действие:", replyMarkup: MoodButtons());
                    }
                    else if (message.Text == ButtonSetMood)
                    {
                        await ShowMoodSelection(client, message.Chat.Id);
                    }
                    else if (message.Text == ButtonGetMoodHistory)
                    {
                        await ShowMoodHistory(client, message.Chat.Id);
                    }
                    else if (message.Text == "Назад")
                    {
                        await client.SendTextMessageAsync(message.Chat.Id, "Вы вернулись к основному меню.", replyMarkup: MyButtonsy());
                    }
                    else
                    {
                        if (userStates.ContainsKey(message.Chat.Id) && userStates[message.Chat.Id] == "awaiting_hr_question")
                        {
                            userStates.Remove(message.Chat.Id);

                            string userQuestion = message.Text;
                            string userName = message.From.Username != null ? $"@{message.From.Username}" : message.From.FirstName;
                            _db.SaveHRRequest(message.From.Username, userQuestion);
                            await client.SendTextMessageAsync(botOwnerID, $"Сообщение от {userName}:\n{userQuestion}");
                            await client.SendTextMessageAsync(message.Chat.Id, "Ок, HR с Вами свяжется, ожидайте.");
                        }
                        else
                        {
                            string userQuestion = message.Text;
                            string answer = FindAnswer(userQuestion);
                            await client.SendTextMessageAsync(message.Chat.Id, answer, parseMode: ParseMode.MarkdownV2);
                        }
                    }
                }
                else if (update.CallbackQuery != null)
                {
                    await HandleCallbackQuery(client, update.CallbackQuery);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка: {ex.Message}");
                if (update.Message != null)
                {
                    await client.SendTextMessageAsync(update.Message.Chat.Id, "Произошла ошибка, попробуйте снова позже.");
                }
                else if (update.CallbackQuery != null)
                {
                    await client.SendTextMessageAsync(update.CallbackQuery.Message.Chat.Id, "Произошла ошибка, попробуйте снова позже.");
                }
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
        private IReplyMarkup MoodButtons()
        {
            return new ReplyKeyboardMarkup(
                new List<List<KeyboardButton>>
                {
            new List<KeyboardButton> // первая строка кнопочек
            {
                new KeyboardButton(ButtonSetMood),
                new KeyboardButton(ButtonGetMoodHistory)
            },
            new List<KeyboardButton> // вторая строка кнопочек
            {
                new KeyboardButton("Назад")
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
                string data = callbackQuery.Data;

                if (data.StartsWith("mood_"))
                {
                    int moodId = int.Parse(data.Substring("mood_".Length));
                    string tgNickname = callbackQuery.From.Username;
                    int employeeId = _db.GetEmployeeId(tgNickname);

                    if (employeeId != -1)
                    {
                        _db.UpsertMoodHistory(employeeId, moodId);
                        await client.SendTextMessageAsync(callbackQuery.Message.Chat.Id, "Ваше настроение было обновлено.");
                    }
                    else
                    {
                        await client.SendTextMessageAsync(callbackQuery.Message.Chat.Id, "Не удалось определить вас как сотрудника.");
                    }

                    await client.DeleteMessageAsync(callbackQuery.Message.Chat.Id, callbackQuery.Message.MessageId);
                }
                else if (_faq.ContainsKey(data))
                {
                    string question = data;
                    string answer = _faq[question];
                    await client.SendTextMessageAsync(callbackQuery.Message.Chat.Id, $"*Вопрос:*\n{question}\n*Ответ:*\n{answer}", parseMode: ParseMode.MarkdownV2);

                    string tgNickname = callbackQuery.From.Username;
                    int employeeId = _db.GetEmployeeId(tgNickname);
                    if (employeeId != -1)
                    {
                        int questionId = _db.GetQuestionId(question);
                        if (questionId != -1)
                        {
                            _db.AddQuestionHistoryRecord(employeeId, questionId);
                        }
                    }

                    await client.DeleteMessageAsync(callbackQuery.Message.Chat.Id, callbackQuery.Message.MessageId);
                }
                else
                {
                    await client.SendTextMessageAsync(callbackQuery.Message.Chat.Id, "Ответ на этот вопрос не найден.");
                    await client.DeleteMessageAsync(callbackQuery.Message.Chat.Id, callbackQuery.Message.MessageId);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при обработке callback-запроса: {ex.Message}");
                await client.SendTextMessageAsync(callbackQuery.Message.Chat.Id, "Произошла ошибка при обработке запроса.");
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
        private async Task ShowMoodSelection(ITelegramBotClient client, long chatId)
        {
            try
            {
                string tgNickname = client.GetChatAsync(chatId).Result.Username;
                if (_db.EmployeeExists(tgNickname))
                {
                    var moods = _db.LoadMoods();
                    if (moods.Count == 0)
                    {
                        await client.SendTextMessageAsync(chatId, "Нет доступных настроений.");
                        return;
                    }

                    var buttons = new List<InlineKeyboardButton>();
                    foreach (var mood in moods)
                    {
                        buttons.Add(InlineKeyboardButton.WithCallbackData(mood.Value, $"mood_{mood.Key}"));
                    }

                    var inlineKeyboard = new InlineKeyboardMarkup(buttons.Select(b => new List<InlineKeyboardButton> { b }));

                    await client.SendTextMessageAsync(chatId, "Выберите ваше настроение:", replyMarkup: inlineKeyboard);
                }
                else
                {
                    await client.SendTextMessageAsync(chatId, "Не удалось определить вас как сотрудника.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при отображении настроений: {ex.Message}");
                await client.SendTextMessageAsync(chatId, "Произошла ошибка при отображении настроений.");
            }
        }
        private async Task ShowMoodHistory(ITelegramBotClient client, long chatId)
        {
            try
            {
                string tgNickname = client.GetChatAsync(chatId).Result.Username;
                int employeeId = _db.GetEmployeeId(tgNickname);

                if (employeeId != -1)
                {
                    var moodHistory = _db.GetMoodHistory(employeeId, 5);
                    if (moodHistory.Count > 0)
                    {
                        string historyText = "Ваше настроение за последние 5 дней:\n\n";
                        foreach (var record in moodHistory)
                        {
                            historyText += $"{record.Key.ToShortDateString()}: {record.Value}\n";
                        }

                        await client.SendTextMessageAsync(chatId, historyText);
                    }
                    else
                    {
                        await client.SendTextMessageAsync(chatId, "Нет записей о вашем настроении за последние 5 дней.");
                    }
                }
                else
                {
                    await client.SendTextMessageAsync(chatId, "Не удалось определить вас как сотрудника.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при получении истории настроений: {ex.Message}");
                await client.SendTextMessageAsync(chatId, "Произошла ошибка при получении истории настроений.");
            }
        }

    }
}
