using System;

namespace GhostlySupaPoc.Config
{
    /// <summary>
    /// Legacy compatibility wrapper for SimpleConfig
    /// Following DRY/KISS principles - just redirects to SimpleConfig
    /// </summary>
    public static class TestConfig
    {
        private static SimpleConfig Config => SimpleConfig.Instance;

        // Supabase Connection
        public static string SupabaseUrl => Config.SupabaseUrl;
        public static string SupabaseAnonKey => Config.SupabaseKey;

        // Storage Bucket
        public static string TestBucket => Config.BucketName;

        // Therapist Test Accounts
        public static string Therapist1Email => Config.TestTherapistEmail;
        public static string Therapist1Password => Config.TestTherapistPassword;
        public static string Therapist2Email => Config.Therapist2Email;
        public static string Therapist2Password => Config.Therapist2Password;
        
        // Aliases for compatibility
        public static string TestTherapistEmail => Config.TestTherapistEmail;
        public static string TestTherapistPassword => Config.TestTherapistPassword;
        
        /// <summary>
        /// Validates that the necessary configuration values are set.
        /// </summary>
        public static bool IsValid()
        {
            return Config.IsValid();
        }
        
        /// <summary>
        /// Validates that configuration for RLS tests is properly set.
        /// </summary>
        public static bool IsRlsConfigValid()
        {
            return Config.IsValid();
        }
    }
}