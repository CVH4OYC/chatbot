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
                var Config = ReadJSON(path);
                var Token = Config["Token"]; // считываем токен

                Host bot = new Host(Token);
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