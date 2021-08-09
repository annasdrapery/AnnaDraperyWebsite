using Nop.Web.Framework.Mvc;

namespace Nop.Web.Models.Checkout
{
    public partial class OnePageCheckoutModel : BaseNopModel
    {
        public bool ShippingRequired { get; set; }
        public bool DisableBillingAddressCheckoutStep { get; set; }
        public bool HideShippingMethod { get; set; }
        public bool HidePaymentMethod { get; set; }
    }
}