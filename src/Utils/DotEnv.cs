using System;
using System.IO;

namespace GhostlySupaPoc.Utils
{
    /// <summary>
    /// Simple .env file loader for local development
    /// </summary>
    public static class DotEnv
    {
        /// <summary>
        /// Load environment variables from .env file
        /// </summary>
        /// <param name="filePath">Path to .env file (default is ".env" in current directory)</param>
        public static void Load(string filePath = ".env")
        {
            if (!File.Exists(filePath))
                return;

            foreach (var line in File.ReadAllLines(filePath))
            {
                // Skip comments and empty lines
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                    continue;

                var parts = line.Split('=', 2, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length != 2)
                    continue;

                var key = parts[0].Trim();
                var value = parts[1].Trim();

                // Remove quotes if present
                if (value.StartsWith("\"") && value.EndsWith("\""))
                {
                    value = value.Substring(1, value.Length - 2);
                }

                // Only set if not already defined in environment
                if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(key)))
                {
                    Environment.SetEnvironmentVariable(key, value);
                }
            }
        }
    }
} 