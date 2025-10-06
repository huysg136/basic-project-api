namespace API.Models.DTO
{
    public class AddToCartDto
    {
        public int UserId { get; set; }
        public int ProductVariantId { get; set; }
        public int Quantity { get; set; }
    }

}
