namespace API.Models.DTO
{
    public class OrderWithPaymentDto
    {
        public int OrderID { get; set; }
        public string CustomerName { get; set; }
        public decimal TotalAmount { get; set; }

        public int? PaymentID { get; set; }
        public int? Method { get; set; }
        public int? Status { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? CollectedAt { get; set; }
    }
}
