namespace API.Models.DTO
{
    public class PaymentCreateDTO
    {
        public int OrderId { get; set; }
        public decimal Amount { get; set; }
        public byte Method { get; set; }
        public string Note { get; set; }
    }
}
