
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
    public class RelayResponseModel : BaseNopModel
    {
        public string ReturnUrl { get; set; }

    }
}
