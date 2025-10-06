namespace API.Models.DTO
{
    public class ProductCreateDto
    {
        public string ProductName { get; set; } = string.Empty; // khởi tạo mặc định
        public string Description { get; set; } = string.Empty;
        public decimal OriginalPrice { get; set; }
        public decimal Price { get; set; }
        public byte Discount { get; set; }
        public int CategoryId { get; set; }
        public string ImagePath { get; set; } = string.Empty;
        public List<ProductVariantDto> ProductVariants { get; set; } = new(); // khởi tạo tránh null
    }
}
