using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;
using Nop.Admin.Models.Common;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Tax;
using Nop.Web.Framework;
using Nop.Web.Framework.Mvc;

namespace Nop.Admin.Models.Orders
{
    public partial class PurchaseOrderModel : BaseNopEntityModel
    {
        public PurchaseOrderModel()
        {
          
        }

        //identifiers
        [NopResourceDisplayName("Admin.PO.Fields.ID")]
        public override int Id { get; set; }
        [NopResourceDisplayName("Admin.PO.Fields.PONumber")]
        public string PONumber { get; set; }
       
        // info
        [NopResourceDisplayName("Admin.PO.Fields.VendorId")]
        public int VendorId { get; set; }
        [NopResourceDisplayName("Admin.PO.Fields.VendorName")]
        public string VendorName { get; set; }

        [NopResourceDisplayName("Admin.PO.Fields.OrderId")]
        public int OrderId { get; set; }

        [NopResourceDisplayName("Admin.PO.Fields.Content")]
        public string Content { get; set; }
      
        //creation date
        [NopResourceDisplayName("Admin.Orders.Fields.CreatedOn")]
        public DateTime CreatedOn { get; set; }

    }
}