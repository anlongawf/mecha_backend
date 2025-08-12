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

        [Required, MaxLength(255)]
        public string Username { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string Email { get; set; } = string.Empty;

        [Required, MaxLength(10)]
        public string Phone { get; set; } = string.Empty;

        [Required]
        public string PassWords { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? Roles { get; set; } = "user";

        [MaxLength(255)]
        public string? DiscordId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("Style")]
        [MaxLength(50)]
        public string? StyleId { get; set; }

        public StyleModel? Style { get; set; }
    }
}