namespace API.Models.DTO
{
    public class ProductUpdateDto
    {
        public string ProductName { get; set; }
        public string Description { get; set; }
        public decimal OriginalPrice { get; set; }
        public decimal Price { get; set; }
        public int Discount { get; set; }
        public int CategoryId { get; set; }
        public string ImagePath { get; set; }

        public List<ProductUpdateVariantDto> ProductVariants { get; set; }
        public List<int> DeletedVariants { get; set; }
    }
}
