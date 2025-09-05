using System;
using Postgrest.Models;
using Postgrest.Attributes;

namespace GhostlySupaPoc.Models
{
    /// <summary>
    /// Represents a patient in the system.
    /// Note: therapist_id now references user_profiles.id (not the old therapists table)
    /// </summary>
    [Table("patients")]
    public class Patient : BaseModel
    {
        [PrimaryKey("id")]
        public Guid Id { get; set; }

        [Column("therapist_id")]
        public Guid TherapistId { get; set; }

        [Column("patient_code")]
        public string PatientCode { get; set; }

        [Column("age_group")]
        public string AgeGroup { get; set; }

        [Column("gender")]
        public string Gender { get; set; }

        [Column("pathology_category")]
        public string PathologyCategory { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }

        [Column("active")]
        public bool Active { get; set; }

        // NOTE: These fields don't exist in the current database schema
        // They're kept here for backward compatibility but will be null
        // The actual schema uses: patient_code, age_group, gender, pathology_category
        
        [Column("first_name")]
        public string FirstName { get; set; }

        [Column("last_name")]
        public string LastName { get; set; }

        [Column("date_of_birth")]
        public DateTime? DateOfBirth { get; set; }
    }
} 