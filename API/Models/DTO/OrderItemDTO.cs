namespace API.Models.DTO
{
    public class OrderItemDTO
    {
        public int VariantId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }
}
