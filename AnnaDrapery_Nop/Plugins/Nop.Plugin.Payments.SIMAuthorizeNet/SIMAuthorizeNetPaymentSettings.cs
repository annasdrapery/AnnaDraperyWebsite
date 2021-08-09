using Nop.Core.Configuration;

namespace Nop.Plugin.Payments.SIMAuthorizeNet
{
    public class SIMAuthorizeNetPaymentSettings : ISettings
    {
        public bool UseSandbox { get; set; }
        public string TransactionKey { get; set; }
        public string LoginId { get; set; }

    }
}
