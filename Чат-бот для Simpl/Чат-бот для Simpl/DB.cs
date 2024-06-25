using System;
using System.Collections.Generic;
using Npgsql;

namespace Чат_бот_для_Simpl
{
    // этот класс для загрузки FAQ из БД и выполнения других операций с базой данных
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

        // Метод для проверки существования сотрудника в таблице Employee
        public bool EmployeeExists(string tgNickname)
        {
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

        // Метод для добавления записи в таблицу Question_History
        public void AddQuestionHistoryRecord(int employeeId, int questionId)
        {
            try
            {
                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    connection.Open();

                    using (var command = new NpgsqlCommand("INSERT INTO Question_History (employee_id, question_id, recording_date) VALUES (@employeeId, @questionId, @recordingDate)", connection))
                    {
                        command.Parameters.AddWithValue("employeeId", employeeId);
                        command.Parameters.AddWithValue("questionId", questionId);
                        command.Parameters.AddWithValue("recordingDate", DateTime.UtcNow);

                        command.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при добавлении записи в историю вопросов: {ex.Message}");
            }
        }

        // Метод для получения id вопроса из таблицы FAQ
        public int GetQuestionId(string question)
        {
            try
            {
                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    connection.Open();

                    using (var command = new NpgsqlCommand("SELECT faq_id FROM faq WHERE question = @question", connection))
                    {
                        command.Parameters.AddWithValue("question", question);
                        var result = command.ExecuteScalar();
                        return result != null ? (int)result : -1;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при получении идентификатора вопроса: {ex.Message}");
                return -1;
            }
        }

        // Метод для получения id сотрудника
        public int GetEmployeeId(string tgNickname)
        {
            try
            {
                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    connection.Open();

                    using (var command = new NpgsqlCommand("SELECT employee_id FROM Employee WHERE tg_nickname = @tgNickname", connection))
                    {
                        command.Parameters.AddWithValue("tgNickname", tgNickname);
                        var result = command.ExecuteScalar();
                        return result != null ? (int)result : -1;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при получении идентификатора сотрудника: {ex.Message}");
                return -1;
            }
        }

        // Получение идентификатора настроения по его названию
        public int GetMoodId(string moodName)
        {
            try
            {
                using (var conn = new NpgsqlConnection(_connectionString))
                {
                    conn.Open();
                    using (var cmd = new NpgsqlCommand("SELECT mood_id FROM mood WHERE mood_name = @name", conn))
                    {
                        cmd.Parameters.AddWithValue("name", moodName);
                        var result = cmd.ExecuteScalar();
                        return result != null ? (int)result : -1;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при получении идентификатора настроения: {ex.Message}");
                return -1;
            }
        }

        // Удаление записи настроения для пользователя на текущий день
        public void DeleteMoodRecordForToday(string tgNickname)
        {
            try
            {
                int employeeId = GetEmployeeId(tgNickname);

                if (employeeId == -1)
                    return;

                using (var conn = new NpgsqlConnection(_connectionString))
                {
                    conn.Open();
                    using (var cmd = new NpgsqlCommand("DELETE FROM mood_record WHERE employee_id = @employeeId AND recording_date = CURRENT_DATE", conn))
                    {
                        cmd.Parameters.AddWithValue("employeeId", employeeId);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при удалении записи настроения: {ex.Message}");
            }
        }

        // Добавление записи настроения
        public void AddMoodRecord(string tgNickname, int moodId)
        {
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                connection.Open();

                int employeeId = GetEmployeeId(tgNickname);
                if (employeeId == -1) return;

                // Проверяем, есть ли запись на сегодняшнюю дату
                if (MoodRecordExistsForToday(employeeId))
                {
                    // Если запись уже есть, то обновляем её
                    UpdateMoodRecordForToday(employeeId, moodId);
                }
                else
                {
                    // Если записи нет, то добавляем новую запись
                    InsertMoodRecordForToday(employeeId, moodId);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при добавлении записи настроения: {ex.Message}");
            }
        }
        private bool MoodRecordExistsForToday(int employeeId)
        {
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                connection.Open();

                using var command = new NpgsqlCommand("SELECT COUNT(*) FROM mood_history WHERE employee_id = @employeeId AND recording_date::date = current_date", connection);
                command.Parameters.AddWithValue("employeeId", employeeId);

                var count = (long)command.ExecuteScalar();
                return count > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при проверке наличия записи настроения: {ex.Message}");
                return false;
            }
        }

        private void UpdateMoodRecordForToday(int employeeId, int moodId)
        {
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                connection.Open();

                using var command = new NpgsqlCommand("UPDATE mood_history SET mood_id = @moodId WHERE employee_id = @employeeId AND recording_date::date = current_date", connection);
                command.Parameters.AddWithValue("moodId", moodId);
                command.Parameters.AddWithValue("employeeId", employeeId);

                command.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при обновлении записи настроения: {ex.Message}");
            }
        }

        private void InsertMoodRecordForToday(int employeeId, int moodId)
        {
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                connection.Open();

                using var command = new NpgsqlCommand("INSERT INTO mood_history (employee_id, mood_id, recording_date) VALUES (@employeeId, @moodId, @recordingDate)", connection);
                command.Parameters.AddWithValue("employeeId", employeeId);
                command.Parameters.AddWithValue("moodId", moodId);
                command.Parameters.AddWithValue("recordingDate", DateTime.UtcNow);

                command.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при добавлении новой записи настроения: {ex.Message}");
            }
        }
    }
}
