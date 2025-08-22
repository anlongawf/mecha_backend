using System.ComponentModel.DataAnnotations.Schema;

namespace Mecha.Models
{
    [Table("user_styles")]
    public class UserStyle
    {
        [Column("style_id")]
        public int StyleId { get; set; }

        [Column("idUser")]
        public int IdUser { get; set; }

        [Column("styles", TypeName = "json")]
        public string Styles { get; set; } // save raw json string
    }
}