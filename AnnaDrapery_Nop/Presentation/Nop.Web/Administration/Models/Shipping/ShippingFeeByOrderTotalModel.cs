using System.Collections.Generic;
using System.Web.Mvc;
using FluentValidation.Attributes;
using Nop.Admin.Validators.Shipping;
using Nop.Web.Framework;
using Nop.Web.Framework.Localization;
using Nop.Web.Framework.Mvc;

namespace Nop.Admin.Models.Shipping
{
   
    public partial class ShippingFeeByOrderTotalModel : BaseNopEntityModel
    {
        public ShippingFeeByOrderTotalModel()
        {
            
        }
        [NopResourceDisplayName("Admin.Configuration.Shipping.ShippingFeeByOrderTotal.Fields.MinValue")]
        public decimal MinValue { get; set; }
        [NopResourceDisplayName("Admin.Configuration.Shipping.ShippingFeeByOrderTotal.Fields.MaxValue")]
        public decimal MaxValue { get; set; }

        [NopResourceDisplayName("Admin.Configuration.Shipping.ShippingFeeByOrderTotal.Fields.ShippingFee")]
        public decimal ShippingFee { get; set; }
    }

}