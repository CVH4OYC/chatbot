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
        }
    }

}
