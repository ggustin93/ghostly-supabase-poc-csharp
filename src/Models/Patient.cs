using System;
using Postgrest.Models;
using Postgrest.Attributes;

namespace GhostlySupaPoc.Models
{
    [Table("patients")]
    public class Patient : BaseModel
    {
        [PrimaryKey("id")]
        public Guid Id { get; set; }

        [Column("therapist_id")]
        public Guid TherapistId { get; set; }

        [Column("first_name")]
        public string FirstName { get; set; }

        [Column("last_name")]
        public string LastName { get; set; }
    }
} 