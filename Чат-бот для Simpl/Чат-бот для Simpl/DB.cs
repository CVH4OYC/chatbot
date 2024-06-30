using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Чат_бот_для_Simpl
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using Npgsql;

    namespace Чат_бот_для_Simpl
    {
        // Класс базы данных для загрузки FAQ и Настроения
        class Database
        {
            private string _connectionString;

            public Database(string connectionString)
            {
                _connectionString = connectionString;
            }

            // Метод для загрузки FAQ из базы данных
            public Dictionary<string, string> LoadFAQ()
            {
                var faq = new Dictionary<string, string>();

                try
                {
                    using (var connection = new NpgsqlConnection(_connectionString))
                    {
                        connection.Open();

                        using (var command = new NpgsqlCommand("SELECT question, answer FROM faq", connection))
                        {
                            using (var reader = command.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    string question = reader.GetString(0);
                                    string answer = reader.GetString(1);
                                    faq[question] = answer;
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка при чтении из базы данных: {ex.Message}");
                }

                return faq;
            }

            // Метод для загрузки настроения из базы данных
            public Dictionary<int, string> LoadMoods()
            {
                var moods = new Dictionary<int, string>();

                try
                {
                    using (var connection = new NpgsqlConnection(_connectionString))
                    {
                        connection.Open();

                        using (var command = new NpgsqlCommand("SELECT mood_id, mood_name FROM Mood", connection))
                        {
                            using (var reader = command.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    int moodId = reader.GetInt32(0);
                                    string moodName = reader.GetString(1);
                                    moods[moodId] = moodName;
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка при чтении из базы данных: {ex.Message}");
                }

                return moods;
            }
            
            //Проверка указывал ли пользователь настроение сегодня
            public bool HasMoodToday(int employeeId)
            {
                try
                {
                    using (var connection = new NpgsqlConnection(_connectionString))
                    {
                        connection.Open();

                        using (var command = new NpgsqlCommand(@"SELECT COUNT(*) FROM Mood_History 
                                                         WHERE employee_id = @employeeId 
                                                         AND recording_date_only = CURRENT_DATE", connection))
                        {
                            command.Parameters.AddWithValue("employeeId", employeeId);
                            var count = (long)command.ExecuteScalar();
                            return count > 0;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка при проверке настроения: {ex.Message}");
                    return false;
                }
            }

            //Обновление и добавление настроения
            public void UpsertMoodHistory(int employeeId, int moodId)
            {
                try
                {
                    using (var connection = new NpgsqlConnection(_connectionString))
                    {
                        connection.Open();

                        using (var command = new NpgsqlCommand(@"
                    INSERT INTO Mood_History (employee_id, mood_id, recording_date)
                    VALUES (@employeeId, @moodId, @recordingDate)
                    ON CONFLICT (employee_id, recording_date)
                    DO UPDATE SET mood_id = EXCLUDED.mood_id, recording_date = EXCLUDED.recording_date", connection))
                        {
                            command.Parameters.AddWithValue("employeeId", employeeId);
                            command.Parameters.AddWithValue("moodId", moodId);
                            command.Parameters.AddWithValue("recordingDate", DateTime.UtcNow);

                            command.ExecuteNonQuery();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка при добавлении или обновлении настроения: {ex.Message}");
                }
            }

            // проверка существования сотрудника в таблице Employee
            public bool EmployeeExists(string tgNickname)
            {
                if (string.IsNullOrEmpty(tgNickname)) return false;
                try
                {
                    using (var connection = new NpgsqlConnection(_connectionString))
                    {
                        connection.Open();

                        using (var command = new NpgsqlCommand("SELECT COUNT(*) FROM employee WHERE tg_nickname = @tgNickname", connection))
                        {
                            command.Parameters.AddWithValue("tgNickname", tgNickname);
                            var count = (long)command.ExecuteScalar();
                            return count > 0;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка при проверке существования сотрудника: {ex.Message}");
                    return false;
                }
            }
            // добавление записи в таблицу Question_History
            public void AddQuestionHistoryRecord(int employeeId, int questionId)
            {
                try
                {
                    using var connection = new NpgsqlConnection(_connectionString);
                    connection.Open();

                    using var command = new NpgsqlCommand("INSERT INTO Question_History (employee_id, question_id, recording_date) VALUES (@employeeId, @questionId, @recordingDate)", connection);
                    command.Parameters.AddWithValue("employeeId", employeeId);
                    command.Parameters.AddWithValue("questionId", questionId);
                    command.Parameters.AddWithValue("recordingDate", DateTime.UtcNow);

                    command.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка при добавлении записи в историю вопросов: {ex.Message}");
                }
            }

            // получение id вопроса из таблицы FAQ
            public int GetQuestionId(string question)
            {
                try
                {
                    using var connection = new NpgsqlConnection(_connectionString);
                    connection.Open();

                    using var command = new NpgsqlCommand("SELECT faq_id FROM faq WHERE question = @question", connection);
                    command.Parameters.AddWithValue("question", question);
                    var result = command.ExecuteScalar();
                    return result != null ? (int)result : -1;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка при получении идентификатора вопроса: {ex.Message}");
                    return -1;
                }
            }
            // получение id сотрудника
            public int GetEmployeeId(string tgNickname)
            {
                if (string.IsNullOrEmpty(tgNickname)) return -1;
                try
                {
                    using var connection = new NpgsqlConnection(_connectionString);
                    connection.Open();

                    using var command = new NpgsqlCommand("SELECT employee_id FROM Employee WHERE tg_nickname = @tgNickname", connection);
                    command.Parameters.AddWithValue("tgNickname", tgNickname);
                    var result = command.ExecuteScalar();
                    return result != null ? (int)result : -1;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка при получении идентификатора сотрудника: {ex.Message}");
                    return -1;
                }
            }
            // метод для получения истории настроения за 5 дней
            public Dictionary<DateTime, string> GetMoodHistory(int employeeId, int days)
            {
                var moodHistory = new Dictionary<DateTime, string>();

                try
                {
                    using (var connection = new NpgsqlConnection(_connectionString))
                    {
                        connection.Open();

                        string query = @"
                SELECT recording_date, mood_name
                FROM Mood_History mh
                JOIN Mood m ON mh.mood_id = m.mood_id
                WHERE employee_id = @employeeId AND recording_date >= CURRENT_DATE - INTERVAL '" + days + @" DAYS'
                ORDER BY recording_date ASC";

                        using (var command = new NpgsqlCommand(query, connection))
                        {
                            command.Parameters.AddWithValue("employeeId", employeeId);

                            using (var reader = command.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    DateTime recordingDate = reader.GetDateTime(0);
                                    string moodName = reader.GetString(1);
                                    moodHistory[recordingDate] = moodName;
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка при чтении истории настроений: {ex.Message}");
                }

                return moodHistory;
            }
            public void SaveHRRequest(string senderTgNickname, string request)
            {
                try
                {
                    using (var connection = new NpgsqlConnection(_connectionString))
                    {
                        connection.Open();
                        using var command = new NpgsqlCommand("INSERT INTO HR_requests_history (sender_tg_nickname, request, recording_date) VALUES (@nickname, @request, @date)", connection);
                        command.Parameters.AddWithValue("nickname", senderTgNickname);
                        command.Parameters.AddWithValue("request", request);
                        command.Parameters.AddWithValue("date", DateTime.UtcNow);
                        command.ExecuteNonQuery();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка при сохранении запроса HR: {ex.Message}");
                }
            }

        }
    }

}
