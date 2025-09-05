using System;
using System.IO;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace GhostlySupaPoc.Config
{
    /// <summary>
    /// Simplified configuration class that loads settings from appsettings.json and environment variables
    /// Follows KISS/DRY/SOLID principles - single source of truth for configuration
    /// </summary>
    public class SimpleConfig
    {
        private static SimpleConfig _instance;
        private static readonly object _lock = new object();

        public string SupabaseUrl { get; set; }
        public string SupabaseKey { get; set; }
        public string BucketName { get; set; } = "emg_data";
        public string TestTherapistEmail { get; set; }
        public string TestTherapistPassword { get; set; }
        public string Therapist2Email { get; set; }
        public string Therapist2Password { get; set; }
        
        // Additional properties for compatibility with old TestConfig
        public string SupabaseAnonKey => SupabaseKey;
        public string TestBucket => BucketName;
        public string Therapist1Email => TestTherapistEmail;
        public string Therapist1Password => TestTherapistPassword;
        
        /// <summary>
        /// Get the singleton instance of configuration
        /// </summary>
        public static SimpleConfig Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = LoadConfiguration();
                        }
                    }
                }
                return _instance;
            }
        }
        
        /// <summary>
        /// Load configuration from appsettings.json or environment variables
        /// Priority: Environment Variables > appsettings.json > defaults
        /// </summary>
        private static SimpleConfig LoadConfiguration()
        {
            var config = new SimpleConfig();
            
            // 1. Try to load from appsettings.json
            var configFile = "appsettings.json";
            if (File.Exists(configFile))
            {
                try
                {
                    var json = File.ReadAllText(configFile);
                    var fileConfig = JsonSerializer.Deserialize<SimpleConfig>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    
                    if (fileConfig != null)
                    {
                        config = fileConfig;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warning: Could not load appsettings.json: {ex.Message}");
                }
            }
            
            // 2. Override with environment variables
            config.SupabaseUrl = Environment.GetEnvironmentVariable("SUPABASE_URL") ?? config.SupabaseUrl;
            config.SupabaseKey = Environment.GetEnvironmentVariable("SUPABASE_ANON_KEY") ?? Environment.GetEnvironmentVariable("SUPABASE_KEY") ?? config.SupabaseKey;
            config.BucketName = Environment.GetEnvironmentVariable("DEFAULT_BUCKET") ?? Environment.GetEnvironmentVariable("BUCKET_NAME") ?? config.BucketName ?? "emg_data";
            config.TestTherapistEmail = Environment.GetEnvironmentVariable("THERAPIST1_EMAIL") ?? Environment.GetEnvironmentVariable("TEST_THERAPIST_EMAIL") ?? config.TestTherapistEmail;
            config.TestTherapistPassword = Environment.GetEnvironmentVariable("THERAPIST1_PASSWORD") ?? Environment.GetEnvironmentVariable("TEST_THERAPIST_PASSWORD") ?? config.TestTherapistPassword;
            config.Therapist2Email = Environment.GetEnvironmentVariable("THERAPIST2_EMAIL") ?? config.Therapist2Email;
            config.Therapist2Password = Environment.GetEnvironmentVariable("THERAPIST2_PASSWORD") ?? config.Therapist2Password;
            
            return config;
        }
        
        /// <summary>
        /// Check if configuration is valid for use
        /// </summary>
        public bool IsValid()
        {
            return !string.IsNullOrEmpty(SupabaseUrl) 
                && !string.IsNullOrEmpty(SupabaseKey)
                && !SupabaseUrl.Contains("your-project")
                && !SupabaseKey.Contains("your-anon-key");
        }
        
        /// <summary>
        /// Get a helpful message about configuration status
        /// </summary>
        public string GetStatusMessage()
        {
            if (IsValid())
            {
                return $"✅ Configuration loaded (URL: {SupabaseUrl.Substring(0, 20)}..., Bucket: {BucketName})";
            }
            else
            {
                return "❌ Configuration not set. Please update appsettings.json or set environment variables:\n" +
                       "   - SUPABASE_URL\n" +
                       "   - SUPABASE_KEY\n" +
                       "   - BUCKET_NAME (optional, defaults to 'emg_data')";
            }
        }
    }
}