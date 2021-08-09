using System;
using System.Collections.Generic;
using Nop.Core.Domain.Orders;

namespace Nop.Core.Domain.Shipping
{
    public partial class ShippingFeeByOrderTotal : BaseEntity
    {
        public decimal MinValue { get; set; }
        public decimal MaxValue { get; set; }
        public decimal ShippingFee { get; set; }
    }
}