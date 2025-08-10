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
        public string Username { get; set; }

        [Required, MaxLength(100)]
        public string Email { get; set; }

        [Required, MaxLength(10)]
        public string Phone { get; set; }

        [Required]
        public string PassWords { get; set; }

        public string Roles { get; set; } = "user";
        public string DiscordId {get;set;}

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}