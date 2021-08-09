using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Net;
using System.Text;
using System.Web.Routing;
using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Directory;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Payments;
using Nop.Core.Plugins;
using Nop.Services.Configuration;
using Nop.Services.Customers;
using Nop.Services.Directory;
using Nop.Services.Localization;
using Nop.Services.Orders;
using Nop.Services.Payments;
using Nop.Services.Security;
using Nop.Plugin.Payments.SIMAuthorizeNet.Controllers;
using System.Web;

namespace Nop.Plugin.Payments.SIMAuthorizeNet
{
    public class SIMAuthorizeNetPaymentProcessor : BasePlugin, IPaymentMethod
    {
        private readonly SIMAuthorizeNetPaymentSettings _simAuthorizeNetPaymentSettings;
        private readonly ISettingService _settingService;
        private readonly ICurrencyService _currencyService;
        private readonly ICustomerService _customerService;
        private readonly CurrencySettings _currencySettings;
        private readonly IWebHelper _webHelper;
        private readonly IOrderTotalCalculationService _orderTotalCalculationService;
        private readonly IEncryptionService _encryptionService;
        public SIMAuthorizeNetPaymentProcessor(SIMAuthorizeNetPaymentSettings simAuthorizeNetPaymentSettings,
            ISettingService settingService,
            ICurrencyService currencyService,
            ICustomerService customerService,
            CurrencySettings currencySettings, IWebHelper webHelper,
            IOrderTotalCalculationService orderTotalCalculationService, IEncryptionService encryptionService)
        {
            this._simAuthorizeNetPaymentSettings = simAuthorizeNetPaymentSettings;
            this._settingService = settingService;
            this._currencyService = currencyService;
            this._customerService = customerService;
            this._currencySettings = currencySettings;
            this._webHelper = webHelper;
            this._orderTotalCalculationService = orderTotalCalculationService;
            this._encryptionService = encryptionService;
        }
        /// <summary>
        /// Gets Authorize.NET URL
        /// </summary>
        /// <returns></returns>
        private string GetAuthorizeNetUrl()
        {
            return _simAuthorizeNetPaymentSettings.UseSandbox ?
                "https://test.authorize.net/gateway/transact.dll" :
                "https://secure2.authorize.net/gateway/transact.dll";
        }

        /// <summary>
        /// Gets Authorize.NET API version
        /// </summary>
        private string GetApiVersion()
        {
            return "1.0";
        }


        #region implement interface
        public bool CanRePostProcessPayment(Order order)
        {
            return false;
        }

        public CancelRecurringPaymentResult CancelRecurringPayment(CancelRecurringPaymentRequest cancelPaymentRequest)
        {
            throw new NotImplementedException();
        }

        public CapturePaymentResult Capture(CapturePaymentRequest capturePaymentRequest)
        {
            throw new NotImplementedException();
        }

        public decimal GetAdditionalHandlingFee(IList<ShoppingCartItem> cart)
        {
            return decimal.Zero;
        }

        public void GetConfigurationRoute(out string actionName, out string controllerName, out RouteValueDictionary routeValues)
        {
            actionName = "Configure";
            controllerName = "PaymentSIMAuthorizeNet";
            routeValues = new RouteValueDictionary { { "Namespaces", "Nop.Plugin.Payments.SIMAuthorizeNet.Controllers" }, { "area", null } };
        }

        public Type GetControllerType()
        {
            return typeof(PaymentSIMAuthorizeNetController);
        }

        public void GetPaymentInfoRoute(out string actionName, out string controllerName, out RouteValueDictionary routeValues)
        {
            actionName = "PaymentInfo";
            controllerName = "PaymentSIMAuthorizeNet";
            routeValues = new RouteValueDictionary { { "Namespaces", "Nop.Plugin.Payments.SIMAuthorizeNet.Controllers" }, { "area", null } };
        }

        public bool HidePaymentMethod(IList<ShoppingCartItem> cart)
        {
            return false;
        }

        public PaymentMethodType PaymentMethodType
        {
            get
            {
                return PaymentMethodType.Redirection;
            }
        }

        public void PostProcessPayment(PostProcessPaymentRequest postProcessPaymentRequest)
        {
            var customer = _customerService.GetCustomerById(postProcessPaymentRequest.Order.CustomerId);
            string post_url = this.GetAuthorizeNetUrl();
            string relay_url = _webHelper.GetStoreLocation(false) + "Plugins/SIMAuthorizeNet/RelayResponse";

            string loginID = _simAuthorizeNetPaymentSettings.LoginId;
            string transactionKey = _simAuthorizeNetPaymentSettings.TransactionKey;
            var orderTotal = Math.Round(postProcessPaymentRequest.Order.OrderTotal, 2);
            string amount = orderTotal.ToString("0.00", CultureInfo.InvariantCulture);
            string invoice = postProcessPaymentRequest.Order.Id.ToString();

            Random random = new Random();
            string sequence = (random.Next(0, 1000)).ToString();
            string timeStamp = ((int)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds).ToString();
            string fingerprint = Utils.HMAC_MD5(transactionKey, loginID + "^" + sequence + "^" + timeStamp + "^" + amount + "^");

            PostPage post_values = new PostPage(post_url);
            post_values.Add("x_login", loginID);
            post_values.Add("x_amount", amount);
            post_values.Add("x_invoice_num", invoice);
            post_values.Add("x_fp_sequence", sequence);
            post_values.Add("x_fp_timestamp", timeStamp);
            post_values.Add("x_fp_hash", fingerprint);
            post_values.Add("x_show_form", "PAYMENT_FORM");
            post_values.Add("x_relay_response", "TRUE");            
            post_values.Add("x_relay_url", relay_url);

            post_values.Add("x_cust_id", customer.Id.ToString());
            post_values.Add("x_email", customer.BillingAddress.Email);
            var cartItems = postProcessPaymentRequest.Order.OrderItems;
            int x = 0;
            decimal tax_shipping_fee = postProcessPaymentRequest.Order.OrderTotal - postProcessPaymentRequest.Order.OrderSubtotalExclTax;
            var lineItem = string.Empty;
            foreach (var item in cartItems)
            {
                x++;
                var product_name = item.Product.Name.Trim().Length > 30 ? (item.Product.Name.Trim().Substring(0, 27) + "...") : item.Product.Name.Trim();
                lineItem += x + "<|>" + product_name + "<|><|>" + item.Quantity + "<|>" + Math.Round(item.UnitPriceExclTax, 2) + "<|>N";
                if (x != cartItems.Count)   
                {
                    lineItem += "<^>";
                }
            }
            post_values.Add("x_line_item", HttpUtility.HtmlAttributeEncode(lineItem));
            if (tax_shipping_fee > 0)
            {
                post_values.Add("x_tax", HttpUtility.HtmlAttributeEncode("Tax1<|><|>" + Math.Round(tax_shipping_fee, 2)));
                post_values.Add("x_rename", HttpUtility.HtmlAttributeEncode("x_tax,Tax + Shipping Fee"));
            }
            
            post_values.Post();

        }


        public ProcessPaymentResult ProcessPayment(ProcessPaymentRequest processPaymentRequest)
        {
            var result = new ProcessPaymentResult();
            result.NewPaymentStatus = PaymentStatus.Pending;
            return result;
        }

        public ProcessPaymentResult ProcessRecurringPayment(ProcessPaymentRequest processPaymentRequest)
        {
            throw new NotImplementedException();
        }

        public RecurringPaymentType RecurringPaymentType
        {
            get { return RecurringPaymentType.NotSupported; }
        }

        public RefundPaymentResult Refund(RefundPaymentRequest refundPaymentRequest)
        {
            throw new NotImplementedException();
        }

        public bool SkipPaymentInfo
        {
            get { return false; }
        }

        public bool SupportCapture
        {
            get
            {
                return false;
            }
        }

        public bool SupportPartiallyRefund
        {
            get
            {
                return false;
            }
        }

        public bool SupportRefund
        {
            get
            {
                return false;
            }
        }

        public bool SupportVoid
        {
            get
            {
                return false;
            }
        }

        public VoidPaymentResult Void(VoidPaymentRequest voidPaymentRequest)
        {
            throw new NotImplementedException();
        }

        public override void Install()
        {
            //settings
            var settings = new SIMAuthorizeNetPaymentSettings
            {
                UseSandbox = true,
                TransactionKey = "123",
                LoginId = "456"
            };
            _settingService.SaveSetting(settings);
            //locales
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.SIMAuthorizeNet.Notes", "If you're using this gateway, ensure that your primary store currency is supported by Authorize.NET.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.SIMAuthorizeNet.Fields.UseSandbox", "Use Sandbox");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.SIMAuthorizeNet.Fields.UseSandbox.Hint", "Check to enable Sandbox (testing environment).");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.SIMAuthorizeNet.Fields.TransactionKey", "Transaction key");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.SIMAuthorizeNet.Fields.TransactionKey.Hint", "Specify transaction key");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.SIMAuthorizeNet.Fields.LoginId", "Login ID");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.SIMAuthorizeNet.Fields.LoginId.Hint", "Specify login identifier.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.SIMAuthorizeNet.Fields.RedirectionTip", "You will be redirected to Authorize.net site to complete the order.");
            base.Install();
        }
        public override void Uninstall()
        {
            //settings
            _settingService.DeleteSetting<SIMAuthorizeNetPaymentSettings>();
            this.DeletePluginLocaleResource("Plugins.Payments.SIMAuthorizeNet.Notes");
            this.DeletePluginLocaleResource("Plugins.Payments.SIMAuthorizeNet.Fields.UseSandbox");
            this.DeletePluginLocaleResource("Plugins.Payments.SIMAuthorizeNet.Fields.UseSandbox.Hint");
            this.DeletePluginLocaleResource("Plugins.Payments.SIMAuthorizeNet.Fields.TransactionKey");
            this.DeletePluginLocaleResource("Plugins.Payments.SIMAuthorizeNet.Fields.TransactionKey.Hint");
            this.DeletePluginLocaleResource("Plugins.Payments.SIMAuthorizeNet.Fields.LoginId");
            this.DeletePluginLocaleResource("Plugins.Payments.SIMAuthorizeNet.Fields.LoginId.Hint");
            this.DeletePluginLocaleResource("Plugins.Payments.SIMAuthorizeNet.Fields.RedirectionTip");
            base.Uninstall();
        }
        #endregion
        #region methods

        #endregion
    }
   
}
