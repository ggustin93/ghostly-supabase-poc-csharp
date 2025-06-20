using System;
using GhostlySupaPoc.Utils;

namespace GhostlySupaPoc.Config
{
    /// <summary>
    /// Centralized configuration class to manage all important variables for the POC.
    /// Reads from environment variables and Repl.it secrets, and provides sensible defaults for testing.
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
        /// <summary>
        /// The default user for running standard client tests.
        /// </summary>
        public static string Therapist1Email { get; }
        public static string Therapist1Password { get; }
        /// <summary>
        /// The second user, primarily for testing cross-therapist RLS policies.
        /// </summary>
        public static string Therapist2Email { get; }
        public static string Therapist2Password { get; }
        
        /// <summary>
        /// Static constructor to initialize configuration from environment variables.
        /// This runs once when the class is first accessed.
        /// Configuration can be set through environment variables or Repl.it Secrets.
        /// </summary>
        static TestConfig()
        {
            // Check if we're on Repl.it
            bool isReplIt = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("REPL_ID"));
            
            if (isReplIt)
            {
                Console.WriteLine("📌 Running on Repl.it - configuration should be set in Secrets tab (tools icon -> Secrets)");
            }
            
            // Load Supabase credentials with default values for development
            SupabaseUrl = PocUtils.GetEnvironmentVariable("SUPABASE_URL", "https://egihfsmxphqcsjotmhmm.supabase.co", required: false);
            SupabaseAnonKey = PocUtils.GetEnvironmentVariable("SUPABASE_ANON_KEY", "xxxx", required: false);

            // Load bucket names (with defaults)
            LegacyTestBucket = PocUtils.GetEnvironmentVariable("LEGACY_TEST_BUCKET", "c3d-files");
            RlsTestBucket = PocUtils.GetEnvironmentVariable("RLS_TEST_BUCKET", "emg_data");

            // Load therapist credentials (with defaults for easy testing)
            Therapist1Email = PocUtils.GetEnvironmentVariable("THERAPIST1_EMAIL", "therapist1@example.com");
            Therapist1Password = PocUtils.GetEnvironmentVariable("THERAPIST1_PASSWORD", "ghostly");
            Therapist2Email = PocUtils.GetEnvironmentVariable("THERAPIST2_EMAIL", "therapist2@example.com");
            Therapist2Password = PocUtils.GetEnvironmentVariable("THERAPIST2_PASSWORD", "ghostly");
        }

        /// <summary>
        /// Validates that the core configuration is present.
        /// </summary>
        /// <returns>True if configuration is valid, otherwise false.</returns>
        public static bool IsValid()
        {
            if (string.IsNullOrEmpty(SupabaseUrl) || string.IsNullOrEmpty(SupabaseAnonKey))
            {
                bool isReplIt = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("REPL_ID"));
                
                if (isReplIt)
                {
                    Console.WriteLine("❌ Missing configuration! SUPABASE_URL and SUPABASE_ANON_KEY must be set.");
                    Console.WriteLine("   To configure on Repl.it:");
                    Console.WriteLine("   1. Click on the tools icon in the left panel");
                    Console.WriteLine("   2. Select 'Secrets'");
                    Console.WriteLine("   3. Add SUPABASE_URL and SUPABASE_ANON_KEY with your values");
                }
                else
                {
                    Console.WriteLine("❌ Missing configuration! SUPABASE_URL and SUPABASE_ANON_KEY must be set.");
                    Console.WriteLine("   Set these as environment variables or in a .env file");
                }
                return false;
            }

            if (!SupabaseUrl.StartsWith("https://"))
            {
                Console.WriteLine("❌ SUPABASE_URL must start with https://");
                return false;
            }
            
            // Check if we're using the default placeholder values
            if (SupabaseUrl == "https://your-project-id.supabase.co" || 
                SupabaseAnonKey == "your-anon-key-goes-here-for-testing")
            {
                Console.WriteLine("⚠️ Using default placeholder values for Supabase configuration.");
                Console.WriteLine("⚠️ Please update with your actual Supabase URL and key for proper functionality.");
                Console.WriteLine("⚠️ You can continue with the demo, but actual Supabase operations will fail.");
                
                // Return true to allow the application to proceed for demo purposes
                return true;
            }
            
            return true;
        }
    }
} 