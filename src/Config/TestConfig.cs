using System;
using GhostlySupaPoc.Utils;

namespace GhostlySupaPoc.Config
{
    /// <summary>
    /// Centralized configuration class to manage all important variables for the POC.
    /// Reads from environment variables and provides sensible defaults for testing.
    /// </summary>
    public static class TestConfig
    {
        // Supabase Connection
        public static string SupabaseUrl { get; }
        public static string SupabaseAnonKey { get; }

        // Storage Buckets
        public static string LegacyTestBucket { get; }
        public static string RlsTestBucket { get; }

        // Therapist Test Accounts
        public static string Therapist1Email { get; }
        public static string Therapist1Password { get; }
        public static string Therapist2Email { get; }
        public static string Therapist2Password { get; }
        
        /// <summary>
        /// Static constructor to initialize configuration from environment variables.
        /// This runs once when the class is first accessed.
        /// </summary>
        static TestConfig()
        {
            // Load Supabase credentials (required)
            SupabaseUrl = PocUtils.GetEnvironmentVariable("SUPABASE_URL", required: true);
            SupabaseAnonKey = PocUtils.GetEnvironmentVariable("SUPABASE_ANON_KEY", required: true);

            // Load bucket names (with defaults)
            LegacyTestBucket = PocUtils.GetEnvironmentVariable("LEGACY_TEST_BUCKET", "c3d-files");
            RlsTestBucket = PocUtils.GetEnvironmentVariable("RLS_TEST_BUCKET", "emg_data");

            // Load therapist credentials (with defaults for easy testing)
            Therapist1Email = PocUtils.GetEnvironmentVariable("THERAPIST1_EMAIL", "therapist1@example.com");
            Therapist1Password = PocUtils.GetEnvironmentVariable("THERAPIST1_PASSWORD", "default_password_1");
            Therapist2Email = PocUtils.GetEnvironmentVariable("THERAPIST2_EMAIL", "therapist2@example.com");
            Therapist2Password = PocUtils.GetEnvironmentVariable("THERAPIST2_PASSWORD", "default_password_2");
        }

        /// <summary>
        /// Validates that the core configuration is present.
        /// </summary>
        /// <returns>True if configuration is valid, otherwise false.</returns>
        public static bool IsValid()
        {
            if (string.IsNullOrEmpty(SupabaseUrl) || string.IsNullOrEmpty(SupabaseAnonKey))
            {
                Console.WriteLine("❌ Missing configuration! SUPABASE_URL and SUPABASE_ANON_KEY must be set.");
                return false;
            }

            if (!SupabaseUrl.StartsWith("https://"))
            {
                Console.WriteLine("❌ SUPABASE_URL must start with https://");
                return false;
            }
            
            return true;
        }
    }
} 