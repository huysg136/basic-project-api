namespace API.Models.DTO
{
    public class ProductVariantDto
    {
        public string Color { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public byte Status { get; set; }
        public string ImagePath { get; set; } = string.Empty;
    }
}
