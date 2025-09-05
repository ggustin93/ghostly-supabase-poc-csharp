using System;
using System.Collections.Generic;
using Postgrest.Attributes;
using Postgrest.Models;

namespace GhostlySupaPoc.Models
{
    /// <summary>
    /// Represents a therapy session in the system, replacing the old emg_sessions table.
    /// Maps to the therapy_sessions table in the database.
    /// </summary>
    [Table("therapy_sessions")]
    public class TherapySession : BaseModel
    {
        [PrimaryKey("id")]
        public Guid Id { get; set; }
        
        [Column("patient_id")]
        public Guid PatientId { get; set; }
        
        [Column("therapist_id")]
        public Guid? TherapistId { get; set; }
        
        [Column("file_path")]
        public string FilePath { get; set; }
        
        [Column("file_hash")]
        public string FileHash { get; set; }
        
        [Column("file_size_bytes")]
        public long FileSizeBytes { get; set; }
        
        [Column("session_date")]
        public DateTime? SessionDate { get; set; }
        
        [Column("session_code")]
        public string SessionCode { get; set; }
        
        [Column("processing_status")]
        public string ProcessingStatus { get; set; }
        
        [Column("processing_error_message")]
        public string ProcessingErrorMessage { get; set; }
        
        [Column("processing_time_ms")]
        public double? ProcessingTimeMs { get; set; }
        
        [Column("game_metadata")]
        public Dictionary<string, object> GameMetadata { get; set; }
        
        [Column("scoring_config_id")]
        public Guid? ScoringConfigId { get; set; }
        
        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
        
        [Column("processed_at")]
        public DateTime? ProcessedAt { get; set; }
        
        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }
        
        // Backward compatibility helpers for old EmgSession references
        public DateTime? RecordedAt => SessionDate;
        public string Notes => GameMetadata?.ContainsKey("notes") == true ? 
            GameMetadata["notes"]?.ToString() : null;
    }
}