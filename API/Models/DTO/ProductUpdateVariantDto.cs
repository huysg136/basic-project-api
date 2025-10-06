namespace API.Models.DTO
{
    public class ProductUpdateVariantDto
    {
        public int? VariantId { get; set; }
        public string Color { get; set; }
        public string ImagePath { get; set; }
        public byte Status { get; set; }
        public bool IsNew { get; set; }
    }
}
