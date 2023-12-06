using System.Runtime.CompilerServices;

namespace MetaFrm.Stock.Console
{
    /// <summary>
    /// Console Utility
    /// </summary>
    public static class Utility
    {
        private static string? lastString = "";
        /// <summary>
        /// ReadCommand
        /// </summary>
        /// <param name="text"></param>
        /// <param name="commandParaString"></param>
        /// <returns></returns>
        public static bool ReadCommand(this string text, out string commandParaString)
        {
            string? commandPara1;

            System.Console.ForegroundColor = ConsoleColor.Green;
            System.Console.Write(text);
            lastString = "> ";
            System.Console.Write(lastString);
            System.Console.ResetColor();
            commandPara1 = System.Console.ReadLine();
            lastString = commandPara1;

            if (!string.IsNullOrEmpty(commandPara1))
            {
                commandParaString = commandPara1;
                return true;
            }

            commandParaString = "";
            return true;
        }

        /// <summary>
        /// ReadCommand
        /// </summary>
        /// <param name="text"></param>
        /// <param name="commandParaInt"></param>
        /// <returns></returns>
        public static bool ReadCommand(this string text, out int commandParaInt)
        {
            string? commandPara1;
            int commandParaInt1 = 0;

            System.Console.ForegroundColor = ConsoleColor.Green;
            System.Console.Write(text);
            lastString = "> ";
            System.Console.Write(lastString);
            System.Console.ResetColor();
            commandPara1 = System.Console.ReadLine();
            lastString = commandPara1;

            if (!string.IsNullOrEmpty(commandPara1) && commandPara1.ToTryInt(out commandParaInt1))
            {
                commandParaInt = commandParaInt1;
                return true;
            }

            commandParaInt = commandParaInt1;
            return false;
        }

        /// <summary>
        /// WriteList
        /// </summary>
        /// <param name="strings"></param>
        public static void WriteList(this List<string> strings)
        {
            System.Console.WriteLine();
            System.Console.ForegroundColor = ConsoleColor.Yellow;
            System.Console.Write($"  {DateTime.Now:dd HH:mm:ss}");
            foreach (string s in strings)
                System.Console.WriteLine($"  {s}");
            lastString = "";
            System.Console.ResetColor();
            System.Console.WriteLine();
        }


        /// <summary>
        /// WriteList
        /// </summary>
        /// <param name="message"></param>
        /// <param name="exchangeID"></param>
        /// <param name="userID"></param>
        /// <param name="settingID"></param>
        /// <param name="market"></param>
        /// <param name="consoleColor"></param>
        public static void WriteMessage(this string message, int? exchangeID = null, int? userID = null, int? settingID = null, string? market = null, ConsoleColor consoleColor = ConsoleColor.White)
        {
            if (message.Contains("주문가능한"))
                return;

            if (consoleColor != ConsoleColor.White)
                System.Console.ForegroundColor = consoleColor;

            if (lastString == "> ")
                System.Console.Write("\r");
            System.Console.WriteLine($"{DateTime.Now:dd HH:mm:ss}{(exchangeID == null ? "": $" ExID:{exchangeID}")}{(userID == null ? "" : $" UID:{userID}")}{(settingID == null ? "" : $" SetID:{settingID}")}{(market == null ? "" : $" MK:{market}")} {message}");

            if (consoleColor != ConsoleColor.White)
                System.Console.ResetColor();
        }

        /// <summary>
        /// WriteMessage
        /// </summary>
        /// <param name="exception"></param>
        /// <param name="methodName"></param>
        /// <param name="detailMessage"></param>
        /// <param name="exchangeID"></param>
        /// <param name="userID"></param>
        /// <param name="settingID"></param>
        /// <param name="market"></param>
        public static void WriteMessage(this Exception exception, bool detailMessage = false, int? exchangeID = null, int? userID = null, int? settingID = null, string? market = null, [CallerMemberName] string? methodName = "")
        {
            if (lastString == "> ")
                System.Console.Write("\r");
            System.Console.ForegroundColor = ConsoleColor.DarkRed;
            System.Console.WriteLine($"{DateTime.Now:dd HH:mm:ss}{(exchangeID == null ? "" : $" ExID:{exchangeID}")}{(userID == null ? "" : $" UID:{userID}")}{(settingID == null ? "" : $" SetID:{settingID}")}{(market == null ? "" : $" MK:{market}")} {methodName} : {(!detailMessage ? exception.Message : exception.ToString())}");
            System.Console.ResetColor();
        }
    }
}