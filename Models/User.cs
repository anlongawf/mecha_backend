using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Mecha.Models
{
    [Table("users")]   
    public class User
    {
        [Key]
        public int IdUser { get; set; }
        [Column("Premium", TypeName = "TINYINT(1)")]
        public bool Premium { get; set; }

        [MaxLength(255)]
        public string? Username { get; set; } // Changed to nullable

        [MaxLength(100)]
        public string? Email { get; set; } // Changed to nullable

        [MaxLength(10)]
        public string? Phone { get; set; } // Changed to nullable

        public string? password { get; set; } // Changed to nullable

        [MaxLength(50)]
        public string? Roles { get; set; } = "user";

        [MaxLength(255)]
        public string? DiscordId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("Style")]
        [MaxLength(50)]
        public string? StyleId { get; set; }

        public StyleModel? Style { get; set; }
        
        [Column("IsVerified", TypeName = "TINYINT(1)")]
        public bool IsVerified { get; set; } = false;
    }
}