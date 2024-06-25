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
        // этот класс для загрузки FAQ из БД
        class Database
        {
            private string _connectionString;

            public Database(string connectionString)
            {
                _connectionString = connectionString;
            }

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
            // проверка существования сотрудника в таблице Employee
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

        }
    }

}
