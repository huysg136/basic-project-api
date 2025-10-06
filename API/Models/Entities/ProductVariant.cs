using System;
using System.Collections.Generic;

namespace API.Models.Entities;

public partial class ProductVariant
{
    public int VariantId { get; set; }

    public int ProductId { get; set; }

    public string Color { get; set; } = null!;

    public string Image { get; set; } = null!;

    public byte Status { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<CartDetail> CartDetails { get; set; } = new List<CartDetail>();

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    public virtual Product Product { get; set; } = null!;
}
