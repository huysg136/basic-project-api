namespace API.Models.DTO
{
    public class OrderDTO
    {
        public int UserId { get; set; }
        public decimal TotalAmount { get; set; }
        public int? DiscountId { get; set; }
        public byte OrderType { get; set; }  // 0: Nhận tại cửa hàng, 1: Giao tận nơi
        public int PaymentMethod { get; set; }  // Thêm dòng này ("cod" hoặc "bank")
        public List<OrderItemDTO> OrderItems { get; set; }
    }
}
