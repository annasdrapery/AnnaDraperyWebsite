using System.Web.Mvc;
using System.Web.Routing;
using Nop.Web.Framework.Mvc.Routes;

namespace Nop.Plugin.Payments.SIMAuthorizeNet
{
    public partial class RouteProvider : IRouteProvider
    {
        public void RegisterRoutes(RouteCollection routes)
        {
            //Relay Response
            routes.MapRoute("Plugin.Payments.SIMAuthorizeNet.RelayResponse",
                 "Plugins/SIMAuthorizeNet/RelayResponse",
                 new { controller = "PaymentSIMAuthorizeNet", action = "RelayResponse" },
                 new[] { "Nop.Plugin.Payments.SIMAuthorizeNet.Controllers" }
            );
        }
        public int Priority
        {
            get
            {
                return 0;
            }
        }
    }
}
