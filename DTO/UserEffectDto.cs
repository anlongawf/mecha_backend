namespace Mecha.DTO
{
    public class UserEffectDto
    {
        public int EffectId { get; set; }
        public int UserId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = "";
        public bool IsActive { get; set; }
        public string AppliedTo { get; set; } = "profile";
        public string? EffectSettings { get; set; }
        public DateTime AppliedAt { get; set; }
    }
}

