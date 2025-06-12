using System;

namespace GhostlySupaPoc.Utils
{
    /// <summary>
    /// Helper class for console output formatting with colors and emojis
    /// </summary>
    public static class ConsoleHelper
    {
        // Common emojis
        public const string CHECK_MARK = "✅";
        public const string CROSS_MARK = "❌";
        public const string WARNING = "⚠️";
        public const string INFO = "ℹ️";
        public const string LOCK = "🔒";
        public const string UNLOCK = "🔓";
        public const string FILE = "📄";
        public const string FOLDER = "📁";
        public const string PERSON = "👤";
        public const string DOCTOR = "👨‍⚕️";
        public const string HOSPITAL = "🏥";
        public const string TEST = "🧪";
        public const string SECURITY = "🛡️";
        public const string DATABASE = "🗄️";
        
        /// <summary>
        /// Write a success message to the console with green color
        /// </summary>
        public static void WriteSuccess(string message)
        {
            WriteColoredLine(message, ConsoleColor.Green, CHECK_MARK);
        }
        
        /// <summary>
        /// Write an error message to the console with red color
        /// </summary>
        public static void WriteError(string message)
        {
            WriteColoredLine(message, ConsoleColor.Red, CROSS_MARK);
        }
        
        /// <summary>
        /// Write a warning message to the console with yellow color
        /// </summary>
        public static void WriteWarning(string message)
        {
            WriteColoredLine(message, ConsoleColor.Yellow, WARNING);
        }
        
        /// <summary>
        /// Write an info message to the console with cyan color
        /// </summary>
        public static void WriteInfo(string message)
        {
            WriteColoredLine(message, ConsoleColor.Cyan, INFO);
        }
        
        /// <summary>
        /// Write a security-related message to the console with magenta color
        /// </summary>
        public static void WriteSecurity(string message)
        {
            WriteColoredLine(message, ConsoleColor.Magenta, SECURITY);
        }
        
        /// <summary>
        /// Write a test-related message to the console with white color
        /// </summary>
        public static void WriteTest(string message)
        {
            WriteColoredLine(message, ConsoleColor.White, TEST);
        }
        
        /// <summary>
        /// Write a section header to the console with blue color
        /// </summary>
        public static void WriteHeader(string message)
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine(message);
            Console.ResetColor();
            Console.WriteLine(new string('-', message.Length));
        }
        
        /// <summary>
        /// Write a major section header to the console with blue background
        /// </summary>
        public static void WriteMajorHeader(string message)
        {
            Console.WriteLine();
            Console.BackgroundColor = ConsoleColor.Blue;
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(message);
            Console.ResetColor();
            Console.WriteLine(new string('=', message.Length));
        }
        
        /// <summary>
        /// Write a colored line to the console with an emoji prefix
        /// </summary>
        public static void WriteColoredLine(string message, ConsoleColor color, string emoji = null)
        {
            Console.ForegroundColor = color;
            Console.WriteLine($"{emoji} {message}".TrimStart());
            Console.ResetColor();
        }
    }
} 