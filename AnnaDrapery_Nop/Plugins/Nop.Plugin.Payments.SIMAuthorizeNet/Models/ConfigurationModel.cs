using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using Nop.Web.Framework;
using Nop.Web.Framework.Mvc;

namespace Nop.Plugin.Payments.SIMAuthorizeNet.Models
{
    public class ConfigurationModel : BaseNopModel
    {
        public int ActiveStoreScopeConfiguration { get; set; }

        [NopResourceDisplayName("Plugins.Payments.SIMAuthorizeNet.Fields.UseSandbox")]
        public bool UseSandbox { get; set; }
        public bool UseSandbox_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Payments.SIMAuthorizeNet.Fields.TransactionKey")]
        public string TransactionKey { get; set; }
        public bool TransactionKey_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Payments.SIMAuthorizeNet.Fields.LoginId")]
        public string LoginId { get; set; }
        public bool LoginId_OverrideForStore { get; set; }

    }
}
