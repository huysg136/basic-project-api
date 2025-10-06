using System;
using System.Collections.Generic;

namespace API.Models.Entities;

public partial class Discount
{
    public int DiscountId { get; set; }

    public string DiscountCode { get; set; } = null!;

    public decimal DiscountValue { get; set; }

    public bool? IsValid { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
}
