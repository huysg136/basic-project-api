namespace API.Models.Request
{
    public class CreatePaymentRequest
    {
        public int OrderId { get; set; }
        public int Amount { get; set; }
    }
}
