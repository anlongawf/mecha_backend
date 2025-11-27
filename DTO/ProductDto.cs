namespace Mecha.DTO
{
    public class ProductDto
    {
        public int ProductId { get; set; }
        public string Name { get; set; } = "";
        public string? Description { get; set; }
        public string Type { get; set; } = "effect";
        public string? Category { get; set; }
        public decimal Price { get; set; }
        public bool PremiumOnly { get; set; }
        public string? Icon { get; set; }
        public string? PreviewImage { get; set; }
        public string? EffectData { get; set; }
        public bool IsActive { get; set; }
        public bool IsOwned { get; set; } = false;
        public bool IsApplied { get; set; } = false;
    }
}

