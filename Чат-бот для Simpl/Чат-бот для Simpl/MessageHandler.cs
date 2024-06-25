using System;
using System.Collections.Generic;
using System.Linq;
using FuzzySharp;
using Npgsql;
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

        private Dictionary<long, string> userStates = new Dictionary<long, string>();

        private Dictionary<string, string> _faq;
        private InlineKeyboardMarkup _faqInlineKeyboard;

        private Database _db;

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
                    userStates[update.Message.Chat.Id] = "awaiting_hr_question";
                }
                else if (update.Message?.Text == ButtonMood)
                {
                    await ShowMoodButtons(client, update.Message.Chat.Id);
                }
                else if (update.CallbackQuery != null)
                {
                    await HandleCallbackQuery(client, update.CallbackQuery);
                }
                else if (!string.IsNullOrEmpty(update.Message?.Text))
                {
                    string userMessage = update.Message.Text;
                    if (userStates.ContainsKey(update.Message.Chat.Id) && userStates[update.Message.Chat.Id] == "awaiting_mood_selection")
                    {
                        // Добавляем запись настроения в базу данных
                        int moodId = _db.GetMoodId(userMessage);
                        if (moodId != -1)
                        {
                            _db.AddMoodRecord(update.Message.From.Username, moodId);
                            await client.SendTextMessageAsync(update.Message.Chat.Id, "Ваше настроение успешно записано!");
                        }
                        else
                        {
                            await client.SendTextMessageAsync(update.Message.Chat.Id, "Извините, настроение не распознано. Выберите настроение из предложенных кнопок.");
                        }

                        userStates.Remove(update.Message.Chat.Id);
                    }
                    else
                    {
                        string answer = FindAnswer(userMessage);
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

        private IReplyMarkup MyButtonsy()
        {
            return new ReplyKeyboardMarkup(
                new List<List<KeyboardButton>>
                {
                    new List<KeyboardButton>
                    {
                        new KeyboardButton(ButtonFAQ),
                        new KeyboardButton(ButtonHR)
                    },
                    new List<KeyboardButton>
                    {
                        new KeyboardButton(ButtonMood)
                    }
                }
            )
            {
                ResizeKeyboard = true
            };
        }

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

        private async Task HandleCallbackQuery(ITelegramBotClient client, CallbackQuery callbackQuery)
        {
            try
            {
                string data = callbackQuery.Data;

                if (_faq.ContainsKey(data))
                {
                    string answer = _faq[data];
                    await client.SendTextMessageAsync(callbackQuery.Message.Chat.Id, $"Вопрос: {data}\nОтвет:\n {answer}", parseMode: ParseMode.MarkdownV2);

                    string tgNickname = callbackQuery.From.Username;
                    int employeeId = _db.GetEmployeeId(tgNickname);
                    if (employeeId != -1)
                    {
                        int questionId = _db.GetQuestionId(data);
                        if (questionId != -1)
                        {
                            _db.AddQuestionHistoryRecord(employeeId, questionId);
                        }
                    }
                }
                else if (data.StartsWith("mood_"))
                {
                    await HandleMoodSelection(client, callbackQuery);
                }
                else
                {
                    await client.SendTextMessageAsync(callbackQuery.Message.Chat.Id, "Ответ на этот вопрос не найден.");
                }

                await client.DeleteMessageAsync(callbackQuery.Message.Chat.Id, callbackQuery.Message.MessageId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при обработке callback-запроса: {ex.Message}");
            }
        }

        private async Task HandleMoodSelection(ITelegramBotClient client, CallbackQuery callbackQuery)
        {
            string mood = callbackQuery.Data.Replace("mood_", "");
            string tgNickname = callbackQuery.From.Username;
            int moodId = _db.GetMoodId(mood);

            if (moodId != -1)
            {
                _db.AddMoodRecord(tgNickname, moodId);
                await client.SendTextMessageAsync(callbackQuery.Message.Chat.Id, $"Ваше настроение '{mood}' было успешно сохранено.");
            }
            else
            {
                await client.SendTextMessageAsync(callbackQuery.Message.Chat.Id, "Произошла ошибка при сохранении настроения.");
            }
        }

        private async Task ShowMoodButtons(ITelegramBotClient client, long chatId)
        {
            var moodButtons = new InlineKeyboardMarkup(new[]
            {
                new[] { InlineKeyboardButton.WithCallbackData("Хорошее", "mood_Хорошее") },
                new[] { InlineKeyboardButton.WithCallbackData("Нейтральное", "mood_Нейтральное") },
                new[] { InlineKeyboardButton.WithCallbackData("Плохое", "mood_Плохое") }
            });

            await client.SendTextMessageAsync(chatId, "Выберите ваше настроение:", replyMarkup: moodButtons);
        }

        private string FindAnswer(string userQuestion)
        {
            if (_faq.TryGetValue(userQuestion, out string directAnswer))
            {
                return directAnswer;
            }

            var fuzzyMatch = _faq.OrderByDescending(qa => Fuzz.Ratio(qa.Key, userQuestion)).FirstOrDefault();
            if (fuzzyMatch.Key != null && Fuzz.Ratio(fuzzyMatch.Key, userQuestion) > 60)
            {
                return fuzzyMatch.Value;
            }

            return "Извините, на такой вопрос пока нет ответа.";
        }
    }
}
