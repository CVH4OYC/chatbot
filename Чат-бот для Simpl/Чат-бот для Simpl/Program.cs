using Telegram.Bot;
namespace Чат_бот_для_Simpl 
{
    class Program
    {
        static void Main()
        {
            Host bot = new Host("7043138381:AAFo4oZ1iJXH0yy9yAUCP5iKnYjgXWzOMtE");
            bot.Start();
            Console.ReadLine();
        }
    }
}