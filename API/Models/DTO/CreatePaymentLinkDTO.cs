namespace API.Models.DTO
{
    public class CreatePaymentLinkDTO
    {
        public int OrderId { get; set; }
        public int Amount { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string ReturnUrl { get; set; } = string.Empty;
        public string CancelUrl { get; set; } = string.Empty;
    }
}
