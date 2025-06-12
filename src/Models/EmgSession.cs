using System;
using Postgrest.Models;
using Postgrest.Attributes;

namespace GhostlySupaPoc.Models
{
    [Table("emg_sessions")]
    public class EmgSession : BaseModel
    {
        [PrimaryKey("id")]
        public Guid Id { get; set; }

        [Column("patient_id")]
        public Guid PatientId { get; set; }

        [Column("therapist_id")]
        public Guid TherapistId { get; set; }

        [Column("file_path")]
        public string FilePath { get; set; }

        [Column("recorded_at")]
        public DateTime RecordedAt { get; set; }

        [Column("notes")]
        public string Notes { get; set; }
    }
} 