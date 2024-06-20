using Telegram.Bot;
using System.Text.Json;

namespace Чат_бот_для_Simpl
{
    class Program
    {
        // конвертируем жсон в словарь
        static Dictionary<string, string> ReadJSON(string path)
        {
            string jsonData = System.IO.File.ReadAllText(path);
            Dictionary<string, string> Config = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonData) ?? new Dictionary<string, string>();
            return Config;
        }

        static void Main()
        {
            try
            {
                // Для безопасности токен бота должен находиться в отдельном файле, который включен в .gitignore
                string path = "../../../Config.JSON";
                var config = ReadJSON(path);
                var token = config["Token"]; // считываем токен
                var faqFilePath = config["FAQFilePath"]; // считываем путь до файла с FAQ


                Host bot = new Host(token,faqFilePath);
                bot.Start();
                Console.ReadLine();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
}