namespace API.Models.DTO
{
    public class PayOSWebhookData
    {
        public int OrderCode { get; set; }
        public int Amount { get; set; }
        public string Status { get; set; }
    }
}
