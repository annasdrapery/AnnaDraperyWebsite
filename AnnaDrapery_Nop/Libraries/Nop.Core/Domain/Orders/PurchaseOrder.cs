using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using Nop.Core.Domain.Common;
using Nop.Core.Domain.Customers;

namespace Nop.Core.Domain.Orders
{
    public partial class PurchaseOrder : BaseEntity
    {
        public string PONumber { get; set; }
        public int OrderId { get; set; }
        public int VendorId { get; set; }
        public string Content {get;set;}
        public bool Deleted { get; set; }
        public DateTime CreatedOnUtc { get; set; }
    }
}
