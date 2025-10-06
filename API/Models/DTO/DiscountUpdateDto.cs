using System.ComponentModel.DataAnnotations;

namespace API.Models.DTO
{
    public class DiscountUpdateDto
    {
        [Required]
        [MaxLength(50)]
        public string DiscountCode { get; set; }

        [Range(0, double.MaxValue)]
        public decimal DiscountValue { get; set; }

        public bool IsValid { get; set; } = true;
    }
}
