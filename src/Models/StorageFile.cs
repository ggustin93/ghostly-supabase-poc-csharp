using System;
using System.Text.Json.Serialization;
using GhostlySupaPoc.Utils; // For the custom converter

namespace GhostlySupaPoc.Models
{
    /// <summary>
    /// Represents a file object from Supabase Storage, specifically for the raw HTTP client.
    /// Includes a custom DateTime converter to handle Supabase's date format.
    /// </summary>
    public class StorageFile
    {
        public string name { get; set; }
        public string id { get; set; }
        public long size { get; set; }
        public string content_type { get; set; }

        [JsonConverter(typeof(SupabaseDateTimeConverter))]
        public DateTime created_at { get; set; }

        [JsonConverter(typeof(SupabaseDateTimeConverter))]
        public DateTime updated_at { get; set; }

        // Compatibility property for existing code that might use the Supabase client's naming convention.
        [JsonIgnore]
        public string Name => name;
    }
} 