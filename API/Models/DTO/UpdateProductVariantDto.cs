namespace API.Models.DTO
{
    public class UpdateProductVariantDto
    {
        public int? VariantId { get; set; } // Thêm ID để phân biệt update/create
        public string Color { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public byte Status { get; set; }
        public string ImagePath { get; set; } = string.Empty;
        public bool IsNew { get; set; } = false; // Flag từ frontend
    }
}
