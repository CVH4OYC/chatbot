using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Чат_бот_для_Simpl
{
    interface IFAQLoader
    {
        Dictionary<string, string> LoadFAQ(string filePath);
    }


    class FileFAQLoader : IFAQLoader
    {
        public Dictionary<string, string> LoadFAQ(string filePath)
        {
            var faq = new Dictionary<string, string>();

            try
            {
                using (var reader = new StreamReader(filePath))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        string[] parts = line.Split('|');
                        if (parts.Length >= 2)
                        {
                            string question = parts[0].Trim();
                            string answer = parts[1].Trim();
                            faq[question] = answer;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при чтении файла: {ex.Message}");
            }

            return faq;
        }
    }
}
