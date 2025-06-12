namespace GhostlySupaPoc.Models
{
    /// <summary>
    /// Represents the authentication response from the Supabase Auth API.
    /// Used by the raw HTTP client.
    /// </summary>
    public class AuthResponse
    {
        public string access_token { get; set; }
        public string token_type { get; set; }
        public int expires_in { get; set; }
        public string refresh_token { get; set; }
        public User user { get; set; }
    }

    public class User
    {
        public string id { get; set; }
        public string email { get; set; }
    }
} 