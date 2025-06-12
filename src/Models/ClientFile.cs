using System;

namespace GhostlySupaPoc.Models
{
    /// <summary>
    /// A unified model representing a file from a storage client.
    /// This acts as a common interface between LegacySupabaseClient and LegacyHttpClient.
    /// </summary>
    public class ClientFile
    {
        public string Name { get; set; }
        public string Id { get; set; }
        public long? Size { get; set; }
        public DateTime? CreatedAt { get; set; }
    }
} 