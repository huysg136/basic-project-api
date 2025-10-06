namespace API.Models.DTO
{
    public class PayOSWebhookRequest
    {
        public string Code { get; set; }
        public string Desc { get; set; }
        public bool Success { get; set; }
        public PayOSWebhookData Data { get; set; }
        public string Signature { get; set; }
    }
}
