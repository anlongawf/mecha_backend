using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Mecha.Models
{
    [Table("style")]
    public class StyleModel
    {
        [Key]
        [Column("style_id")]
        [MaxLength(50)]
        [Required] // StyleId should not be null as it's the primary key
        public string StyleId { get; set; } = string.Empty;

        [Column("profile_avatar")]
        [MaxLength(255)]
        public string? ProfileAvatar { get; set; }

        [Column("background")]
        [MaxLength(255)]
        public string? Background { get; set; }

        [Column("audio")]
        [MaxLength(255)]
        public string? Audio { get; set; }

        [Column("custom_cursor")]
        [MaxLength(255)]
        public string? CustomCursor { get; set; }

        [Column("description")]
        [MaxLength(500)]
        public string? Description { get; set; }

        [Column("username")]
        [MaxLength(100)]
        public string? Username { get; set; }

        [Column("effect_username")]
        [MaxLength(255)]
        public string? EffectUsername { get; set; }

        [Column("location")]
        [MaxLength(255)]
        public string? Location { get; set; }
    }
}