using System;
using Postgrest.Attributes;
using Postgrest.Models;

namespace GhostlySupaPoc.Models
{
    /// <summary>
    /// Represents a user profile in the system, replacing the old therapists table.
    /// Maps to the user_profiles table in the database.
    /// </summary>
    [Table("user_profiles")]
    public class UserProfile : BaseModel
    {
        [PrimaryKey("id")]
        public Guid Id { get; set; }
        
        [Column("role")]
        public string Role { get; set; }
        
        [Column("first_name")]
        public string FirstName { get; set; }
        
        [Column("last_name")]
        public string LastName { get; set; }
        
        [Column("full_name")]
        public string FullName { get; set; }
        
        [Column("full_name_override")]
        public string FullNameOverride { get; set; }
        
        [Column("institution")]
        public string Institution { get; set; }
        
        [Column("department")]
        public string Department { get; set; }
        
        [Column("access_level")]
        public string AccessLevel { get; set; }
        
        [Column("active")]
        public bool Active { get; set; }
        
        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
        
        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }
        
        [Column("last_login")]
        public DateTime? LastLogin { get; set; }
    }
}