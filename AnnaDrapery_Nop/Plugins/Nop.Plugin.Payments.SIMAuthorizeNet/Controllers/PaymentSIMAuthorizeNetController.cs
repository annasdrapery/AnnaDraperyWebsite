using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using Nop.Core;
using Nop.Core.Domain.Orders;
using Nop.Plugin.Payments.SIMAuthorizeNet.Models;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Orders;
using Nop.Services.Payments;
using Nop.Services.Stores;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;

namespace Nop.Plugin.Payments.SIMAuthorizeNet.Controllers
{
    public class PaymentSIMAuthorizeNetController : BasePaymentController
    {
        private readonly IWorkContext _workContext;
        private readonly IStoreService _storeService;
        private readonly ISettingService _settingService;
        private readonly ILocalizationService _localizationService;
        private readonly IOrderService _orderService;
        private readonly IOrderProcessingService _orderProcessingService;
        private readonly IWebHelper _webHelper;

        public PaymentSIMAuthorizeNetController(IWorkContext workContext,
            IStoreService storeService, 
            ISettingService settingService, 
            IOrderService orderService,
            IOrderProcessingService orderProcessingService,
            IWebHelper webHelper,
            ILocalizationService localizationService)
        {
            this._workContext = workContext;
            this._storeService = storeService;
            this._settingService = settingService;
            this._localizationService = localizationService;
            this._orderService = orderService;
            this._orderProcessingService = orderProcessingService;
            this._webHelper = webHelper;
        }
        
        [AdminAuthorize]
        [ChildActionOnly]
        public ActionResult Configure()
        {
            //load settings for a chosen store scope
            var storeScope = this.GetActiveStoreScopeConfiguration(_storeService, _workContext);
            var authorizeNetPaymentSettings = _settingService.LoadSetting<SIMAuthorizeNetPaymentSettings>(storeScope);

            var model = new ConfigurationModel();
            model.UseSandbox = authorizeNetPaymentSettings.UseSandbox;
            model.TransactionKey = authorizeNetPaymentSettings.TransactionKey;
            model.LoginId = authorizeNetPaymentSettings.LoginId;

            model.ActiveStoreScopeConfiguration = storeScope;
            if (storeScope > 0)
            {
                model.UseSandbox_OverrideForStore = _settingService.SettingExists(authorizeNetPaymentSettings, x => x.UseSandbox, storeScope);
                model.TransactionKey_OverrideForStore = _settingService.SettingExists(authorizeNetPaymentSettings, x => x.TransactionKey, storeScope);
                model.LoginId_OverrideForStore = _settingService.SettingExists(authorizeNetPaymentSettings, x => x.LoginId, storeScope);
            }

            return View("~/Plugins/Payments.SIMAuthorizeNet/Views/PaymentSIMAuthorizeNet/Configure.cshtml", model);
        }

        [HttpPost]
        [AdminAuthorize]
        [ChildActionOnly]
        public ActionResult Configure(ConfigurationModel model)
        {
            if (!ModelState.IsValid)
                return Configure();

            //load settings for a chosen store scope
            var storeScope = this.GetActiveStoreScopeConfiguration(_storeService, _workContext);
            var authorizeNetPaymentSettings = _settingService.LoadSetting<SIMAuthorizeNetPaymentSettings>(storeScope);

            //save settings
            authorizeNetPaymentSettings.UseSandbox = model.UseSandbox;
            authorizeNetPaymentSettings.TransactionKey = model.TransactionKey;
            authorizeNetPaymentSettings.LoginId = model.LoginId;

            /* We do not clear cache after each setting update.
             * This behavior can increase performance because cached settings will not be cleared 
             * and loaded from database after each update */
            if (model.UseSandbox_OverrideForStore || storeScope == 0)
                _settingService.SaveSetting(authorizeNetPaymentSettings, x => x.UseSandbox, storeScope, false);
            else if (storeScope > 0)
                _settingService.DeleteSetting(authorizeNetPaymentSettings, x => x.UseSandbox, storeScope);


            if (model.TransactionKey_OverrideForStore || storeScope == 0)
                _settingService.SaveSetting(authorizeNetPaymentSettings, x => x.TransactionKey, storeScope, false);
            else if (storeScope > 0)
                _settingService.DeleteSetting(authorizeNetPaymentSettings, x => x.TransactionKey, storeScope);

            if (model.LoginId_OverrideForStore || storeScope == 0)
                _settingService.SaveSetting(authorizeNetPaymentSettings, x => x.LoginId, storeScope, false);
            else if (storeScope > 0)
                _settingService.DeleteSetting(authorizeNetPaymentSettings, x => x.LoginId, storeScope);

           
            //now clear settings cache
            _settingService.ClearCache();

            SuccessNotification(_localizationService.GetResource("Admin.Plugins.Saved"));

            return Configure();
        }


        public override ProcessPaymentRequest GetPaymentInfo(FormCollection form)
        {
            var paymentInfo = new ProcessPaymentRequest();
            return paymentInfo;
        }

        public override IList<string> ValidatePaymentForm(FormCollection form)
        {
            var warnings = new List<string>();
            return warnings;
        }
        [ChildActionOnly]
        public ActionResult PaymentInfo()
        {
            return View("~/Plugins/Payments.SIMAuthorizeNet/Views/PaymentSIMAuthorizeNet/PaymentInfo.cshtml");
        }
        [ValidateInput(false)]
        public ActionResult RelayResponse(FormCollection form)
        {
            string responseCode;

            if (Request["x_invoice_num"] == null)
            {
                throw new NopException("Cannot retrieve x_invoice_num");
            }
            responseCode = form["x_response_code"];
            var x_response_reason_text = form["x_response_reason_text"];
            var x_response_reason_code = form["x_response_reason_code"];
            var x_invoice_num = form["x_invoice_num"];
            var x_auth_code = form["x_auth_code"];
            var x_trans_id = form["x_trans_id"];
            var x_amount = form["x_amount"];
            var x_type = form["x_type"];
            var x_account_number = form["x_account_number"];
            var x_card_type = form["x_card_type"];

            var sb = new StringBuilder();
            sb.AppendLine("SIM Authorize.net:");
            sb.AppendLine("x_response_code: " + responseCode);
            sb.AppendLine("x_response_reason_text: " + x_response_reason_text);
            sb.AppendLine("x_response_reason_code: " + x_response_reason_code);
            sb.AppendLine("x_invoice_num: " + x_invoice_num);
            sb.AppendLine("x_auth_code: " + x_auth_code);
            sb.AppendLine("x_trans_id: " + x_trans_id);
            sb.AppendLine("x_amount: " + x_amount);
            sb.AppendLine("x_type: " + x_type);
            sb.AppendLine("x_account_number: " + x_account_number);
            sb.AppendLine("x_card_type: " + x_card_type);
            //order note
            var order = _orderService.GetOrderById(int.Parse(x_invoice_num));
            if (order == null)
            {
                throw new NopException("Cannot retrieve Order with id=" + x_invoice_num);
            }
            order.OrderNotes.Add(new OrderNote
            {
                Note = sb.ToString(),
                DisplayToCustomer = false,
                CreatedOnUtc = DateTime.UtcNow
            });
            _orderService.UpdateOrder(order);
            if (responseCode == "1")//success
            {
                if (_orderProcessingService.CanMarkOrderAsAuthorized(order))
                {
                    order.AuthorizationTransactionId = x_trans_id;
                    order.AuthorizationTransactionCode = x_auth_code;
                    order.AuthorizationTransactionResult = x_response_reason_text;
                    order.OrderStatusId = (int)OrderStatus.Processing;
                    order.OrderNotes.Add(new OrderNote
                    {
                        Note = "Order status has been changed to Processing",
                        DisplayToCustomer = false,
                        CreatedOnUtc = DateTime.UtcNow
                    });
                    _orderService.UpdateOrder(order);
                    _orderProcessingService.MarkAsAuthorized(order);
                    //send notification
                    _orderProcessingService.SendMainNotification(order);
                }

            }
            else // fail
            {
                if (_orderProcessingService.CanMarkOrderAsAuthorized(order))
                {
                    order.AuthorizationTransactionId = x_trans_id;
                    order.AuthorizationTransactionCode = x_auth_code;
                    order.AuthorizationTransactionResult = x_response_reason_text;
                    _orderService.UpdateOrder(order);
                    //send notification

                }

            }
            var model = new RelayResponseModel()
            {
                ReturnUrl = _webHelper.GetStoreLocation(false) + "orderdetails/" + order.Id
            };
            return View("~/Plugins/Payments.SIMAuthorizeNet/Views/PaymentSIMAuthorizeNet/RelayResponse.cshtml", model);
        }
    }
}