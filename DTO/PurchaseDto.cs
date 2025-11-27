namespace Mecha.DTO
{
    public class PurchaseDto
    {
        public int PurchaseId { get; set; }
        public int UserId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = "";
        public decimal Price { get; set; }
        public string? PaymentMethod { get; set; }
        public string? TransactionId { get; set; }
        public string Status { get; set; } = "completed";
        public DateTime PurchasedAt { get; set; }
    }
}

