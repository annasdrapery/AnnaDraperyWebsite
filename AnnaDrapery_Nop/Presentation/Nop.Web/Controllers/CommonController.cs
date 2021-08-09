using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using DocumentFormat.OpenXml.Packaging;
using Newtonsoft.Json;
using Nop.Core;
using Nop.Core.Caching;
using Nop.Core.Domain;
using Nop.Core.Domain.Blogs;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Common;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Forums;
using Nop.Core.Domain.Localization;
using Nop.Core.Domain.Messages;
using Nop.Core.Domain.News;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Tax;
using Nop.Core.Domain.Vendors;
using Nop.Services.Catalog;
using Nop.Services.Common;
using Nop.Services.Customers;
using Nop.Services.Directory;
using Nop.Services.Forums;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Messages;
using Nop.Services.Orders;
using Nop.Services.Security;
using Nop.Services.Seo;
using Nop.Services.Topics;
using Nop.Services.Vendors;
using Nop.Web.Extensions;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Localization;
using Nop.Web.Framework.Security;
using Nop.Web.Framework.Security.Captcha;
using Nop.Web.Framework.Themes;
using Nop.Web.Infrastructure.Cache;
using Nop.Web.Models.Catalog;
using Nop.Web.Models.Common;
using Nop.Web.Models.Topics;
using OfficeOpenXml;
using Rotativa.Core.Options;

namespace Nop.Web.Controllers
{
    public partial class CommonController : BasePublicController
    {
        #region Fields

        private readonly ICategoryService _categoryService;
        private readonly IProductService _productService;
        private readonly IManufacturerService _manufacturerService;
        private readonly ITopicService _topicService;
        private readonly ILanguageService _languageService;
        private readonly ICurrencyService _currencyService;
        private readonly ILocalizationService _localizationService;
        private readonly IWorkContext _workContext;
        private readonly IStoreContext _storeContext;
        private readonly IQueuedEmailService _queuedEmailService;
        private readonly IEmailAccountService _emailAccountService;
        private readonly ISitemapGenerator _sitemapGenerator;
        private readonly IThemeContext _themeContext;
        private readonly IThemeProvider _themeProvider;
        private readonly IForumService _forumservice;
        private readonly IGenericAttributeService _genericAttributeService;
        private readonly IWebHelper _webHelper;
        private readonly IPermissionService _permissionService;
        private readonly ICacheManager _cacheManager;
        private readonly ICustomerActivityService _customerActivityService;
        private readonly IVendorService _vendorService;

        private readonly CustomerSettings _customerSettings;
        private readonly TaxSettings _taxSettings;
        private readonly CatalogSettings _catalogSettings;
        private readonly StoreInformationSettings _storeInformationSettings;
        private readonly EmailAccountSettings _emailAccountSettings;
        private readonly CommonSettings _commonSettings;
        private readonly BlogSettings _blogSettings;
        private readonly NewsSettings _newsSettings;
        private readonly ForumSettings _forumSettings;
        private readonly LocalizationSettings _localizationSettings;
        private readonly CaptchaSettings _captchaSettings;
        private readonly VendorSettings _vendorSettings;

        #endregion

        #region Constructors

        public CommonController(ICategoryService categoryService,
            IProductService productService,
            IManufacturerService manufacturerService,
            ITopicService topicService,
            ILanguageService languageService,
            ICurrencyService currencyService,
            ILocalizationService localizationService,
            IWorkContext workContext,
            IStoreContext storeContext,
            IQueuedEmailService queuedEmailService,
            IEmailAccountService emailAccountService,
            ISitemapGenerator sitemapGenerator,
            IThemeContext themeContext,
            IThemeProvider themeProvider,
            IForumService forumService,
            IGenericAttributeService genericAttributeService,
            IWebHelper webHelper,
            IPermissionService permissionService,
            ICacheManager cacheManager,
            ICustomerActivityService customerActivityService,
            IVendorService vendorService,
            CustomerSettings customerSettings,
            TaxSettings taxSettings,
            CatalogSettings catalogSettings,
            StoreInformationSettings storeInformationSettings,
            EmailAccountSettings emailAccountSettings,
            CommonSettings commonSettings,
            BlogSettings blogSettings,
            NewsSettings newsSettings,
            ForumSettings forumSettings,
            LocalizationSettings localizationSettings,
            CaptchaSettings captchaSettings,
            VendorSettings vendorSettings)
        {
            this._categoryService = categoryService;
            this._productService = productService;
            this._manufacturerService = manufacturerService;
            this._topicService = topicService;
            this._languageService = languageService;
            this._currencyService = currencyService;
            this._localizationService = localizationService;
            this._workContext = workContext;
            this._storeContext = storeContext;
            this._queuedEmailService = queuedEmailService;
            this._emailAccountService = emailAccountService;
            this._sitemapGenerator = sitemapGenerator;
            this._themeContext = themeContext;
            this._themeProvider = themeProvider;
            this._forumservice = forumService;
            this._genericAttributeService = genericAttributeService;
            this._webHelper = webHelper;
            this._permissionService = permissionService;
            this._cacheManager = cacheManager;
            this._customerActivityService = customerActivityService;
            this._vendorService = vendorService;

            this._customerSettings = customerSettings;
            this._taxSettings = taxSettings;
            this._catalogSettings = catalogSettings;
            this._storeInformationSettings = storeInformationSettings;
            this._emailAccountSettings = emailAccountSettings;
            this._commonSettings = commonSettings;
            this._blogSettings = blogSettings;
            this._newsSettings = newsSettings;
            this._forumSettings = forumSettings;
            this._localizationSettings = localizationSettings;
            this._captchaSettings = captchaSettings;
            this._vendorSettings = vendorSettings;
        }

        #endregion

        #region Utilities

        [NonAction]
        protected virtual int GetUnreadPrivateMessages()
        {
            var result = 0;
            var customer = _workContext.CurrentCustomer;
            if (_forumSettings.AllowPrivateMessages && !customer.IsGuest())
            {
                var privateMessages = _forumservice.GetAllPrivateMessages(_storeContext.CurrentStore.Id,
                    0, customer.Id, false, null, false, string.Empty, 0, 1);

                if (privateMessages.TotalCount > 0)
                {
                    result = privateMessages.TotalCount;
                }
            }

            return result;
        }

        #endregion

        #region Methods

        //page not found
        public ActionResult PageNotFound()
        {
            this.Response.StatusCode = 404;
            this.Response.TrySkipIisCustomErrors = true;

            return View();
        }

        //language
        [ChildActionOnly]
        public ActionResult LanguageSelector()
        {
            var availableLanguages = _cacheManager.Get(string.Format(ModelCacheEventConsumer.AVAILABLE_LANGUAGES_MODEL_KEY, _storeContext.CurrentStore.Id), () =>
            {
                var result = _languageService
                    .GetAllLanguages(storeId: _storeContext.CurrentStore.Id)
                    .Select(x => new LanguageModel
                    {
                        Id = x.Id,
                        Name = x.Name,
                        FlagImageFileName = x.FlagImageFileName,
                    })
                    .ToList();
                return result;
            });

            var model = new LanguageSelectorModel
            {
                CurrentLanguageId = _workContext.WorkingLanguage.Id,
                AvailableLanguages = availableLanguages,
                UseImages = _localizationSettings.UseImagesForLanguageSelection
            };

            if (model.AvailableLanguages.Count == 1)
                Content("");

            return PartialView(model);
        }
        //available even when a store is closed
        [StoreClosed(true)]
        //available even when navigation is not allowed
        [PublicStoreAllowNavigation(true)]
        public ActionResult SetLanguage(int langid, string returnUrl = "")
        {
            var language = _languageService.GetLanguageById(langid);
            if (language != null && language.Published)
            {
                _workContext.WorkingLanguage = language;
            }

            //home page
            if (String.IsNullOrEmpty(returnUrl))
                returnUrl = Url.RouteUrl("HomePage");

            //prevent open redirection attack
            if (!Url.IsLocalUrl(returnUrl))
                returnUrl = Url.RouteUrl("HomePage");

            //language part in URL
            if (_localizationSettings.SeoFriendlyUrlsForLanguagesEnabled)
            {
                string applicationPath = HttpContext.Request.ApplicationPath;
                if (returnUrl.IsLocalizedUrl(applicationPath, true))
                {
                    //already localized URL
                    returnUrl = returnUrl.RemoveLanguageSeoCodeFromRawUrl(applicationPath);
                }
                returnUrl = returnUrl.AddLanguageSeoCodeToRawUrl(applicationPath, _workContext.WorkingLanguage);
            }
            return Redirect(returnUrl);
        }

        //currency
        [ChildActionOnly]
        public ActionResult CurrencySelector()
        {
            var availableCurrencies = _cacheManager.Get(string.Format(ModelCacheEventConsumer.AVAILABLE_CURRENCIES_MODEL_KEY, _workContext.WorkingLanguage.Id, _storeContext.CurrentStore.Id), () =>
            {
                var result = _currencyService
                    .GetAllCurrencies(storeId: _storeContext.CurrentStore.Id)
                    .Select(x =>
                    {
                        //currency char
                        var currencySymbol = "";
                        if (!string.IsNullOrEmpty(x.DisplayLocale))
                            currencySymbol = new RegionInfo(x.DisplayLocale).CurrencySymbol;
                        else
                            currencySymbol = x.CurrencyCode;
                        //model
                        var currencyModel = new CurrencyModel
                        {
                            Id = x.Id,
                            Name = x.GetLocalized(y => y.Name),
                            CurrencySymbol = currencySymbol
                        };
                        return currencyModel;
                    })
                    .ToList();
                return result;
            });

            var model = new CurrencySelectorModel
            {
                CurrentCurrencyId = _workContext.WorkingCurrency.Id,
                AvailableCurrencies = availableCurrencies
            };

            if (model.AvailableCurrencies.Count == 1)
                Content("");

            return PartialView(model);
        }
        //available even when navigation is not allowed
        [PublicStoreAllowNavigation(true)]
        public ActionResult SetCurrency(int customerCurrency, string returnUrl = "")
        {
            var currency = _currencyService.GetCurrencyById(customerCurrency);
            if (currency != null)
                _workContext.WorkingCurrency = currency;

            //home page
            if (String.IsNullOrEmpty(returnUrl))
                returnUrl = Url.RouteUrl("HomePage");

            //prevent open redirection attack
            if (!Url.IsLocalUrl(returnUrl))
                returnUrl = Url.RouteUrl("HomePage");

            return Redirect(returnUrl);
        }

        //tax type
        [ChildActionOnly]
        public ActionResult TaxTypeSelector()
        {
            if (!_taxSettings.AllowCustomersToSelectTaxDisplayType)
                return Content("");

            var model = new TaxTypeSelectorModel
            {
                CurrentTaxType = _workContext.TaxDisplayType
            };

            return PartialView(model);
        }
        //available even when navigation is not allowed
        [PublicStoreAllowNavigation(true)]
        public ActionResult SetTaxType(int customerTaxType, string returnUrl = "")
        {
            var taxDisplayType = (TaxDisplayType)Enum.ToObject(typeof(TaxDisplayType), customerTaxType);
            _workContext.TaxDisplayType = taxDisplayType;

            //home page
            if (String.IsNullOrEmpty(returnUrl))
                returnUrl = Url.RouteUrl("HomePage");

            //prevent open redirection attack
            if (!Url.IsLocalUrl(returnUrl))
                returnUrl = Url.RouteUrl("HomePage");

            return Redirect(returnUrl);
        }

        //footer
        [ChildActionOnly]
        public ActionResult JavaScriptDisabledWarning()
        {
            if (!_commonSettings.DisplayJavaScriptDisabledWarning)
                return Content("");

            return PartialView();
        }

        //header links
        [ChildActionOnly]
        public ActionResult HeaderLinks()
        {
            var customer = _workContext.CurrentCustomer;

            var unreadMessageCount = GetUnreadPrivateMessages();
            var unreadMessage = string.Empty;
            var alertMessage = string.Empty;
            if (unreadMessageCount > 0)
            {
                unreadMessage = string.Format(_localizationService.GetResource("PrivateMessages.TotalUnread"), unreadMessageCount);

                //notifications here
                if (_forumSettings.ShowAlertForPM &&
                    !customer.GetAttribute<bool>(SystemCustomerAttributeNames.NotifiedAboutNewPrivateMessages, _storeContext.CurrentStore.Id))
                {
                    _genericAttributeService.SaveAttribute(customer, SystemCustomerAttributeNames.NotifiedAboutNewPrivateMessages, true, _storeContext.CurrentStore.Id);
                    alertMessage = string.Format(_localizationService.GetResource("PrivateMessages.YouHaveUnreadPM"), unreadMessageCount);
                }
            }

            var model = new HeaderLinksModel
            {
                IsAuthenticated = customer.IsRegistered(),
                CustomerEmailUsername = customer.IsRegistered() ? (_customerSettings.UsernamesEnabled ? customer.Username : customer.Email) : "",
                ShoppingCartEnabled = _permissionService.Authorize(StandardPermissionProvider.EnableShoppingCart),
                WishlistEnabled = _permissionService.Authorize(StandardPermissionProvider.EnableWishlist),
                AllowPrivateMessages = customer.IsRegistered() && _forumSettings.AllowPrivateMessages,
                UnreadPrivateMessages = unreadMessage,
                AlertMessage = alertMessage,
            };
            //performance optimization (use "HasShoppingCartItems" property)
            if (customer.HasShoppingCartItems)
            {
                model.ShoppingCartItems = customer.ShoppingCartItems
                    .Where(sci => sci.ShoppingCartType == ShoppingCartType.ShoppingCart)
                    .LimitPerStore(_storeContext.CurrentStore.Id)
                    .ToList()
                    .GetTotalProducts();
                model.WishlistItems = customer.ShoppingCartItems
                    .Where(sci => sci.ShoppingCartType == ShoppingCartType.Wishlist)
                    .LimitPerStore(_storeContext.CurrentStore.Id)
                    .ToList()
                    .GetTotalProducts();
            }

            return PartialView(model);
        }
        [ChildActionOnly]
        public ActionResult AdminHeaderLinks()
        {
            var customer = _workContext.CurrentCustomer;

            var model = new AdminHeaderLinksModel
            {
                ImpersonatedCustomerEmailUsername = customer.IsRegistered() ? (_customerSettings.UsernamesEnabled ? customer.Username : customer.Email) : "",
                IsCustomerImpersonated = _workContext.OriginalCustomerIfImpersonated != null,
                DisplayAdminLink = _permissionService.Authorize(StandardPermissionProvider.AccessAdminPanel),
            };

            return PartialView(model);
        }
        [ChildActionOnly]
        public ActionResult TopHeader()
        {
            //model
            var model = new FooterModel
            {
                FacebookLink = _storeInformationSettings.FacebookLink,
                TwitterLink = _storeInformationSettings.TwitterLink,
                YoutubeLink = _storeInformationSettings.YoutubeLink,
                GooglePlusLink = _storeInformationSettings.GooglePlusLink,
                IsAuthenticated = _workContext.CurrentCustomer.IsRegistered()
            };

            return PartialView(model);
        }
        //footer
        [ChildActionOnly]
        public ActionResult Footer()
        {
            //footer topics
            string topicCacheKey = string.Format(ModelCacheEventConsumer.TOPIC_FOOTER_MODEL_KEY,
                _workContext.WorkingLanguage.Id,
                _storeContext.CurrentStore.Id,
                string.Join(",", _workContext.CurrentCustomer.GetCustomerRoleIds()));
            var cachedTopicModel = _cacheManager.Get(topicCacheKey, () =>
                _topicService.GetAllTopics(_storeContext.CurrentStore.Id)
                .Where(t => t.IncludeInFooterColumn1 || t.IncludeInFooterColumn2 || t.IncludeInFooterColumn3)
                .Select(t => new FooterModel.FooterTopicModel
                {
                    Id = t.Id,
                    Name = t.GetLocalized(x => x.Title),
                    SeName = t.GetSeName(),
                    IncludeInFooterColumn1 = t.IncludeInFooterColumn1,
                    IncludeInFooterColumn2 = t.IncludeInFooterColumn2,
                    IncludeInFooterColumn3 = t.IncludeInFooterColumn3
                })
                .ToList()
            );

            //model
            var model = new FooterModel
            {
                StoreName = _storeContext.CurrentStore.GetLocalized(x => x.Name),
                WishlistEnabled = _permissionService.Authorize(StandardPermissionProvider.EnableWishlist),
                ShoppingCartEnabled = _permissionService.Authorize(StandardPermissionProvider.EnableShoppingCart),
                SitemapEnabled = _commonSettings.SitemapEnabled,
                WorkingLanguageId = _workContext.WorkingLanguage.Id,
                FacebookLink = _storeInformationSettings.FacebookLink,
                TwitterLink = _storeInformationSettings.TwitterLink,
                YoutubeLink = _storeInformationSettings.YoutubeLink,
                GooglePlusLink = _storeInformationSettings.GooglePlusLink,
                BlogEnabled = _blogSettings.Enabled,
                CompareProductsEnabled = _catalogSettings.CompareProductsEnabled,
                ForumEnabled = _forumSettings.ForumsEnabled,
                NewsEnabled = _newsSettings.Enabled,
                RecentlyViewedProductsEnabled = _catalogSettings.RecentlyViewedProductsEnabled,
                NewProductsEnabled = _catalogSettings.NewProductsEnabled,
                DisplayTaxShippingInfoFooter = _catalogSettings.DisplayTaxShippingInfoFooter,
                HidePoweredByNopCommerce = _storeInformationSettings.HidePoweredByNopCommerce,
                AllowCustomersToApplyForVendorAccount = _vendorSettings.AllowCustomersToApplyForVendorAccount,
                Topics = cachedTopicModel
            };

            return PartialView(model);
        }
        // catalogs page
        [NopHttpsRequirement(SslRequirement.Yes)]
        //available even when a store is closed
        [StoreClosed(true)]
        public ActionResult Catalogs()
        {
            return View();
        }
        // price list page
        [NopHttpsRequirement(SslRequirement.Yes)]
        //available even when a store is closed
        [StoreClosed(true)]
        public ActionResult PriceList()
        {
            return View();
        }
        private byte[] GetFile(string s)
        {
            byte[] data;
            using (System.IO.FileStream fs = System.IO.File.OpenRead(s))
            {
                data = new byte[fs.Length];
                int br = fs.Read(data, 0, data.Length);
                if (br != fs.Length)
                    throw new System.IO.IOException(s);
            }
            return data;
        }

        [NopHttpsRequirement(SslRequirement.Yes)]
        //available even when a store is closed
        [StoreClosed(true)]
        public ActionResult DesignerLoginLanding()
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.AccessPagesUnderDesignerLogin))
            {
                return RedirectToAction("Login", "Customer", new { returnUrl = "/designer-login-landing" });
            }
            return View();
        }
        [NopHttpsRequirement(SslRequirement.Yes)]
        //available even when a store is closed
        [StoreClosed(true)]
        [HttpPost]
        public ActionResult PriceList(string password)
        {
            var _downloadPwd = ConfigurationManager.AppSettings["DownloadPwd"];
            var _docUrl = "~/ANNADRAPERY2016PRICELIST.pdf";
            if (!String.IsNullOrEmpty(ConfigurationManager.AppSettings["PriceListUrl"]))
            {
                _docUrl = ConfigurationManager.AppSettings["PriceListUrl"];
            }
            if (_downloadPwd == password)
            {
                string filepath = Server.MapPath(_docUrl);
                var binaryFile = GetFile(filepath);

                string fileName = _docUrl.Substring(_docUrl.LastIndexOf('/') + 1);
                string contentType = "application/octet-stream";
                return new FileContentResult(binaryFile, contentType) { FileDownloadName = fileName };
                //return Redirect(_docUrl);
            }
            else
            {
                ViewBag.Message = "Wrong Password. Please try again";
            }
            return View();
        }
        // rt rtb drapery order form page
        [NopHttpsRequirement(SslRequirement.Yes)]
        //available even when a store is closed
        [StoreClosed(true)]
        public ActionResult RTRTBDraperyOrderForm()
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.AccessPagesUnderDesignerLogin))
            {
                return RedirectToAction("Login", "Customer", new { returnUrl = "/rt-rtb-drapery-order-form" });
            }
            var customer = _workContext.CurrentCustomer;
            var model = new DraperyOrderFormModel()
            {
                DisplayCaptcha = _captchaSettings.Enabled,
                Designer = customer.GetFullName()
            };
            return View(model);
        }
        [NopHttpsRequirement(SslRequirement.Yes)]
        [HttpPost, ParameterBasedOnFormName("print", "isPrint")]
        [PublicAntiForgery]
        [CaptchaValidator]
        //available even when a store is closed
        [StoreClosed(true)]
        public ActionResult RTRTBDraperyOrderForm(DraperyOrderFormModel model, bool captchaValid, bool isPrint)
        {
            //validate CAPTCHA
            if (_captchaSettings.Enabled && !captchaValid)
            {
                ModelState.AddModelError("", _captchaSettings.GetWrongCaptchaMessage(_localizationService));
            }
            if (ModelState.IsValid)
            {
                var details = JsonConvert.DeserializeObject<DraperyOrderDetailModel[]>(model.OrderDetailJsonStr);
                var isExportFileSuccess = false;
                var fileName = "RT RBT ORDER FORM_" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".xlsx";
                var exportFolder = "exportfiles";
                bool exists = System.IO.Directory.Exists(Server.MapPath(exportFolder));
                if (!exists)
                    System.IO.Directory.CreateDirectory(Server.MapPath(exportFolder));
                var templateFilePath = Server.MapPath("RT RBT ORDER FORM.xlsx");
                var destinationFilePath = Server.MapPath(exportFolder + "\\" + fileName);
                if (System.IO.File.Exists(templateFilePath))
                {
                    var templateFile = new FileInfo(templateFilePath);
                    ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                    using (var package = new ExcelPackage(templateFile))
                    {
                        // Get the work book in the file
                        var workBook = package.Workbook;
                        if (workBook != null)
                        {
                            if (workBook.Worksheets.Count > 0)
                            {
                                // Get the first worksheet
                                var currentWorksheet = workBook.Worksheets.First();
                                currentWorksheet.Cells["AM3"].Value = model.Designer;
                                currentWorksheet.Cells["CA3"].Value = model.OrderDate;
                                currentWorksheet.Cells["AM4"].Value = model.SideMark;
                                currentWorksheet.Cells["CA4"].Value = model.DueDate;
                                currentWorksheet.Cells["AM6"].Value = model.Phone;
                                var i = 10;
                                foreach (var dt in details)
                                {
                                    i++;
                                    currentWorksheet.Cells["C" + i].Value = dt.RoomLocation;
                                    currentWorksheet.Cells["O" + i].Value = dt.Qty;
                                    
                                    currentWorksheet.Cells["V" + i].Value = dt.FinishedWidth;
                                    currentWorksheet.Cells["AD" + i].Value = dt.NoOfWidth;
                                    currentWorksheet.Cells["AE" + i].Value = dt.FinishedLength;
                                    currentWorksheet.Cells["AF" + i].Value = dt.TopHeader;
                                    currentWorksheet.Cells["AM" + i].Value = dt.TopPocket;
                                    currentWorksheet.Cells["AT" + i].Value = dt.BottomHeader;
                                    currentWorksheet.Cells["BA" + i].Value = dt.BottomPocket;

                                    currentWorksheet.Cells["BH" + i].Value = dt.FabricNameColor;
                                    currentWorksheet.Cells["CB" + i].Value = dt.LiningNameColor;
                                    
                                }
                            }
                            package.SaveAs(new FileInfo(destinationFilePath));
                            isExportFileSuccess = true;
                        }
                    }
                }
                if (isExportFileSuccess)
                {
                    if (isPrint)
                    {
                        ViewBag.DownloadFileUrl = String.Format("/{0}/{1}", exportFolder, fileName);
                        return View(model);
                    }
                    //send mail
                    var subject = "[Anna Drapery Site] New RT & RTB Drape Order Form request was sent to you";
                    var emailAccount = _emailAccountService.GetEmailAccountById(_emailAccountSettings.DefaultEmailAccountId);
                    if (emailAccount == null)
                        emailAccount = _emailAccountService.GetAllEmailAccounts().FirstOrDefault();
                    if (emailAccount == null)
                        throw new Exception("No email account could be loaded");
                    var from = emailAccount.Email;
                    var fromName = emailAccount.DisplayName;
                    var body = "Hi Admin, <br/>a customer submitted the RT & RTB Drape Order Form in Anna Drapery site. Please view the attachment<br/>Thanks<br/> Anna Drapery site";
                    var adminMail = _localizationService.GetResource("admin.emails");
                    _queuedEmailService.InsertQueuedEmail(new QueuedEmail
                    {
                        From = from,
                        FromName = fromName,
                        To = adminMail,
                        ToName = emailAccount.DisplayName,
                        Priority = QueuedEmailPriority.High,
                        Subject = subject,
                        Body = body,
                        CreatedOnUtc = DateTime.UtcNow,
                        EmailAccountId = emailAccount.Id,
                        AttachmentFileName = fileName,
                        AttachmentFilePath = destinationFilePath
                    });

                    return View("CompleteForm", new CompleteFormModel()
                    {
                        Title = "RT & RTB Drape Order Form",
                        DownloadUrl = String.Format("/{0}/{1}", exportFolder, fileName),
                        ResultText = "Thank you! Your order has been sent to the DATA ENTRY"
                    });
                }
                else
                {
                    ViewBag.Message = "Cannot export file. Please contact to administrator for more detail";
                }
            }
            else
            {
                ViewBag.Message = "Please input the required fields";
            }
            return View(model);
        }
        // pleated drapery order form page
        [NopHttpsRequirement(SslRequirement.Yes)]
        //available even when a store is closed
        [StoreClosed(true)]
        public ActionResult DraperyOrderForm()
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.AccessPagesUnderDesignerLogin))
            {
                return RedirectToAction("Login", "Customer", new { returnUrl = "/drapery-order-form" });
            }
            var customer = _workContext.CurrentCustomer;
            var model = new DraperyOrderFormModel()
            {
                DisplayCaptcha = _captchaSettings.Enabled,
                Designer = customer.GetFullName()
            };
            return View(model);
        }

        [NopHttpsRequirement(SslRequirement.Yes)]
        [HttpPost, ParameterBasedOnFormName("print", "isPrint")]
        [PublicAntiForgery]
        [CaptchaValidator]
        //available even when a store is closed
        [StoreClosed(true)]
        public ActionResult DraperyOrderForm(DraperyOrderFormModel model, bool captchaValid, bool isPrint)
        {
            //validate CAPTCHA
            if (_captchaSettings.Enabled && !captchaValid)
            {
                ModelState.AddModelError("", _captchaSettings.GetWrongCaptchaMessage(_localizationService));
            }
            if (ModelState.IsValid)
            {
                var details = JsonConvert.DeserializeObject<DraperyOrderDetailModel[]>(model.OrderDetailJsonStr);
                var isExportFileSuccess = false;
                var fileName = "PLEATED DRAPES ORDER 2020 experience_" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".xlsx";
                var exportFolder = "exportfiles";
                bool exists = System.IO.Directory.Exists(Server.MapPath(exportFolder));
                if (!exists)
                    System.IO.Directory.CreateDirectory(Server.MapPath(exportFolder));
                var templateFilePath = Server.MapPath("PLEATED DRAPES ORDER 2020 experience.xlsx");
                var destinationFilePath = Server.MapPath(exportFolder + "\\" + fileName);
                if (System.IO.File.Exists(templateFilePath))
                {
                    var templateFile = new FileInfo(templateFilePath);
                    ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                    using (var package = new ExcelPackage(templateFile))
                    {
                        // Get the work book in the file
                        var workBook = package.Workbook;
                        if (workBook != null)
                        {
                            if (workBook.Worksheets.Count > 0)
                            {
                                // Get the first worksheet
                                var currentWorksheet = workBook.Worksheets.First();
                                currentWorksheet.Cells["AH3"].Value = model.Designer;
                                currentWorksheet.Cells["BU3"].Value = model.OrderDate;
                                currentWorksheet.Cells["AH4"].Value = model.SideMark;
                                currentWorksheet.Cells["BU4"].Value = model.DueDate;
                                currentWorksheet.Cells["AH6"].Value = model.Phone;
                                currentWorksheet.Cells["AL21"].Value = model.Note;
                                var i = 0;
                                foreach (var dt in details)
                                {
                                    i++;
                                    currentWorksheet.Cells["C1" + i].Value = dt.RoomLocation;
                                    currentWorksheet.Cells["L1" + i].Value = dt.Qty;
                                    currentWorksheet.Cells["O1" + i].Value = dt.Style;
                                    switch (dt.Type)
                                    {
                                        case "1 Way Left":
                                            currentWorksheet.Cells["U1" + i].Value = dt.BRBR;
                                            break;
                                        case "1 Way Right":
                                            currentWorksheet.Cells["Y1" + i].Value = dt.BRBR;
                                            break;
                                        case "Split Draw":
                                            currentWorksheet.Cells["AC1" + i].Value = dt.BRBR;
                                            break;
                                        default:
                                            break;
                                    }

                                    currentWorksheet.Cells["AH1" + i].Value = dt.Return;
                                    currentWorksheet.Cells["AL1" + i].Value = dt.Overlap;
                                    currentWorksheet.Cells["AP1" + i].Value = dt.Hoodset;
                                    currentWorksheet.Cells["AS1" + i].Value = dt.Fullness;
                                    currentWorksheet.Cells["AW1" + i].Value = dt.FinishedLength;

                                    currentWorksheet.Cells["C" + (21 + i)].Value = dt.FabricNameColor;
                                    currentWorksheet.Cells["U" + (21 + i)].Value = dt.LiningNameColor;

                                    currentWorksheet.Cells["BB1" + i].Value = dt.FinishedWidth;
                                    currentWorksheet.Cells["BG1" + i].Value = dt.LeftNoWidth;
                                    currentWorksheet.Cells["BK1" + i].Value = dt.RightNoWidth;
                                    currentWorksheet.Cells["BO1" + i].Value = dt.LeftSpace;
                                    currentWorksheet.Cells["BT1" + i].Value = dt.LeftNoOfPleats;
                                    currentWorksheet.Cells["BY1" + i].Value = dt.LeftPleat;
                                    currentWorksheet.Cells["CD1" + i].Value = dt.RightSpace;
                                    currentWorksheet.Cells["CI1" + i].Value = dt.RightNoOfPleats;
                                    currentWorksheet.Cells["CN1" + i].Value = dt.RightPleat;

                                }
                            }
                            package.SaveAs(new FileInfo(destinationFilePath));
                            isExportFileSuccess = true;
                        }
                    }
                }
                if (isExportFileSuccess)
                {
                    if (isPrint)
                    {
                        ViewBag.DownloadFileUrl = String.Format("/{0}/{1}", exportFolder, fileName);
                        return View(model);
                    }
                    //send mail
                    var subject = "[Anna Drapery Site] New Drapery Order Form request was sent to you";
                    var emailAccount = _emailAccountService.GetEmailAccountById(_emailAccountSettings.DefaultEmailAccountId);
                    if (emailAccount == null)
                        emailAccount = _emailAccountService.GetAllEmailAccounts().FirstOrDefault();
                    if (emailAccount == null)
                        throw new Exception("No email account could be loaded");
                    var from = emailAccount.Email;
                    var fromName = emailAccount.DisplayName;
                    var body = "Hi Admin, <br/>a customer submitted the Drapery Order Form in Anna Drapery site. Please view the attachment<br/>Thanks<br/> Anna Drapery site";
                    var adminMail = _localizationService.GetResource("admin.emails");
                    _queuedEmailService.InsertQueuedEmail(new QueuedEmail
                    {
                        From = from,
                        FromName = fromName,
                        To = adminMail,
                        ToName = emailAccount.DisplayName,
                        Priority = QueuedEmailPriority.High,
                        Subject = subject,
                        Body = body,
                        CreatedOnUtc = DateTime.UtcNow,
                        EmailAccountId = emailAccount.Id,
                        AttachmentFileName = fileName,
                        AttachmentFilePath = destinationFilePath
                    });

                    return View("CompleteForm", new CompleteFormModel()
                    {
                        Title = "Drapery Order Form",
                        DownloadUrl = String.Format("/{0}/{1}", exportFolder, fileName),
                        ResultText = "Thank you! Your order has been sent to the DATA ENTRY"
                    });
                }
            }
            else
            {
                ViewBag.Message = "Please input the required fields";
            }
            return View(model);
        }
        // drapery order form page
        [NopHttpsRequirement(SslRequirement.Yes)]
        //available even when a store is closed
        [StoreClosed(true)]
        public ActionResult TopTreatmentsAndShades()
        {
            return View();
        }

        [NopHttpsRequirement(SslRequirement.Yes)]
        //available even when a store is closed
        [StoreClosed(true)]
        [HttpPost]
        public ActionResult TopTreatmentsAndShades(string password)
        {
            var _downloadPwd = ConfigurationManager.AppSettings["DownloadPwd"];
            var _docUrl = "~/TOP_TREATMENTS_SHADES.xlsx";
            if (!String.IsNullOrEmpty(ConfigurationManager.AppSettings["TreatmentUrl"]))
            {
                _docUrl = ConfigurationManager.AppSettings["TreatmentUrl"];
            }
            if (_downloadPwd == password)
            {
                return Redirect(_docUrl);
            }
            else
            {
                ViewBag.Message = "Wrong Password. Please try again";
            }
            return View();
        }

        [NopHttpsRequirement(SslRequirement.Yes)]
        //available even when a store is closed
        [StoreClosed(true)]
        public ActionResult CustomerCreditApplication()
        {
            var model = new CustomerCreditApplicationModel()
            {
                DisplayCaptcha = _captchaSettings.Enabled
            };
            return View(model);
        }
        [NopHttpsRequirement(SslRequirement.Yes)]
        //available even when a store is closed
        [StoreClosed(true)]
        [HttpPost]
        [PublicAntiForgery]
        [CaptchaValidator]
        public ActionResult CustomerCreditApplication(CustomerCreditApplicationModel model, bool captchaValid)
        {
            //validate CAPTCHA
            if (_captchaSettings.Enabled && !captchaValid)
            {
                ModelState.AddModelError("", _captchaSettings.GetWrongCaptchaMessage(_localizationService));
            }
            if (ModelState.IsValid)
            {
                switch (model.SelectedText)
                {
                    case "SPS":
                        model.SoleProprietorship = true;
                        break;
                    case "PNS":
                        model.Partnership = true;
                        break;
                    case "COR":
                        model.Corporation = true;
                        break;
                    case "LLC":
                        model.LLC = true;
                        break;
                    default:
                        break;
                }

                var exportToWord = false;
                var isExportFileSuccess = false;
                var destinationFilePath = string.Empty;
                var fileName = "CUSTOMER CREDIT APPLICATION_" + DateTime.Now.ToString("yyyyMMddHHmmss");
                var exportFolder = "exportfiles";
                bool exists = System.IO.Directory.Exists(Server.MapPath(exportFolder));
                if (!exists)
                    System.IO.Directory.CreateDirectory(Server.MapPath(exportFolder));

                if (exportToWord) //export to word
                {
                    fileName += ".docx";
                    var templateFilePath = Server.MapPath("CUSTOMER CREDIT APPLICATION_template.docx");
                    destinationFilePath = Server.MapPath(exportFolder + "\\" + fileName);
                    if (System.IO.File.Exists(templateFilePath))
                    {
                        System.IO.File.Copy(templateFilePath, destinationFilePath);
                        if (System.IO.File.Exists(destinationFilePath))
                        {
                            using (WordprocessingDocument doc = WordprocessingDocument.Open(destinationFilePath, true))
                            {
                                string docText = null;
                                using (StreamReader sr = new StreamReader(doc.MainDocumentPart.GetStream()))
                                {
                                    docText = sr.ReadToEnd();
                                }

                                docText = docText.Replace("{{Comp}}", model.CompanyName);
                                docText = docText.Replace("{{Address}}", model.Address);
                                docText = docText.Replace("{{City}}", model.City);
                                docText = docText.Replace("{{State}}", model.State);
                                docText = docText.Replace("{{ZipCode}}", model.ZipCode);
                                docText = docText.Replace("{{S}}", (model.SoleProprietorship ? "[X]" : "[ ]"));
                                docText = docText.Replace("{{P}}", (model.Partnership ? "[X]" : "[ ]"));
                                docText = docText.Replace("{{C}}", (model.Corporation ? "[X]" : "[ ]"));
                                docText = docText.Replace("{{L}}", (model.LLC ? "[X]" : "[ ]"));
                                docText = docText.Replace("{{STID}}", model.StateTaxID);
                                docText = docText.Replace("{{Phone}}", model.Phone);
                                docText = docText.Replace("{{Cell}}", model.Cell);
                                docText = docText.Replace("{{Email}}", model.Email);
                                docText = docText.Replace("{{Fax}}", model.Fax);
                                docText = docText.Replace("{{Company1}}", model.Company1);
                                docText = docText.Replace("{{Address1}}", model.Address1);
                                docText = docText.Replace("{{Phone1}}", model.Phone1);
                                docText = docText.Replace("{{Fax1}}", model.Fax1);
                                docText = docText.Replace("{{Company2}}", model.Company2);
                                docText = docText.Replace("{{Address2}}", model.Address2);
                                docText = docText.Replace("{{Phone2}}", model.Phone2);
                                docText = docText.Replace("{{Fax2}}", model.Fax2);


                                using (StreamWriter sw = new StreamWriter(doc.MainDocumentPart.GetStream(FileMode.Create)))
                                {
                                    sw.Write(docText);
                                }
                                isExportFileSuccess = true;
                            }
                        }
                        else
                        {
                            ViewBag.Message = "Cannot generate word file. Please contact to administrator";
                        }
                    }
                    else
                    {
                        ViewBag.Message = "Cannot find the template file. Please contact to administrator";
                    }

                }
                else //export to pdf
                {

                    fileName += ".pdf";
                    destinationFilePath = Server.MapPath(exportFolder + "\\" + fileName);
                    var report = new Rotativa.MVC.ViewAsPdf("CustomerCreditApplicationExportView", model)
                    {
                        RotativaOptions = new Rotativa.Core.DriverOptions()
                        {
                            PageSize = Rotativa.Core.Options.Size.A4,
                            PageOrientation = Orientation.Portrait,
                            IsLowQuality = true,
                            PageMargins = new Margins(0, 0, 0, 0)
                        },
                    };
                    byte[] byteArray = report.BuildPdf(ControllerContext);
                    using (var fileStream = new FileStream(destinationFilePath, FileMode.Create, FileAccess.Write))
                    {
                        fileStream.Write(byteArray, 0, byteArray.Length);
                    }

                    isExportFileSuccess = true;
                }

                if (isExportFileSuccess)
                {
                    //send mail
                    var subject = "[Anna Drapery Site] New Customer Credit Application request was sent to you";
                    var emailAccount = _emailAccountService.GetEmailAccountById(_emailAccountSettings.DefaultEmailAccountId);
                    if (emailAccount == null)
                        emailAccount = _emailAccountService.GetAllEmailAccounts().FirstOrDefault();
                    if (emailAccount == null)
                        throw new Exception("No email account could be loaded");
                    var from = emailAccount.Email;
                    var fromName = emailAccount.DisplayName;
                    var body = "Hi Admin, <br/>a customer submitted the Customer Credit Application form in Anna Drapery site. Please view the attachment<br/>Thanks<br/> Anna Drapery site";
                    var adminMail = _localizationService.GetResource("admin.emails");
                    _queuedEmailService.InsertQueuedEmail(new QueuedEmail
                    {
                        From = from,
                        FromName = fromName,
                        To = adminMail,
                        ToName = emailAccount.DisplayName,
                        Priority = QueuedEmailPriority.High,
                        Subject = subject,
                        Body = body,
                        CreatedOnUtc = DateTime.UtcNow,
                        EmailAccountId = emailAccount.Id,
                        AttachmentFileName = fileName,
                        AttachmentFilePath = destinationFilePath
                    });

                    return View("CompleteForm", new CompleteFormModel()
                    {
                        Title = "Customer Credit Application",
                        DownloadUrl = String.Format("/{0}/{1}", exportFolder, fileName)
                    });
                }

            }
            else
            {
                ViewBag.Message = "Please input the required fields";
            }
            return View(model);
        }
        [NopHttpsRequirement(SslRequirement.Yes)]
        //available even when a store is closed
        [StoreClosed(true)]
        public ActionResult SaleTaxCertificate()
        {
            var model = new SaleTaxCertificateModel()
            {
                DisplayCaptcha = _captchaSettings.Enabled
            };
            return View(model);
        }
        [NopHttpsRequirement(SslRequirement.Yes)]
        //available even when a store is closed
        [StoreClosed(true)]
        [HttpPost]
        [PublicAntiForgery]
        [CaptchaValidator]
        public ActionResult SaleTaxCertificate(SaleTaxCertificateModel model, bool captchaValid)
        {
            //validate CAPTCHA
            if (_captchaSettings.Enabled && !captchaValid)
            {
                ModelState.AddModelError("", _captchaSettings.GetWrongCaptchaMessage(_localizationService));
            }
            if (ModelState.IsValid)
            {
                var fileName = "Sale_Tax_Certificate_" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".pdf";
                var exportFolder = "exportfiles";
                var destinationFilePath = Server.MapPath(exportFolder + "\\" + fileName);
                bool exists = System.IO.Directory.Exists(Server.MapPath("exportfiles"));

                if (!exists)
                    System.IO.Directory.CreateDirectory(Server.MapPath("exportfiles"));
                var report = new Rotativa.MVC.ViewAsPdf("SaleTaxCertificateExportView", model)
                {
                    RotativaOptions = new Rotativa.Core.DriverOptions()
                    {
                        PageSize = Rotativa.Core.Options.Size.A4,
                        PageOrientation = Orientation.Portrait,
                        IsLowQuality = true,
                        PageMargins = new Margins(0, 15, 0, 0)
                    },
                };
                byte[] byteArray = report.BuildPdf(ControllerContext);
                using (var fileStream = new FileStream(destinationFilePath, FileMode.Create, FileAccess.Write))
                {
                    fileStream.Write(byteArray, 0, byteArray.Length);
                }
                //send mail
                var subject = "[Anna Drapery Site] New Sale Tax Certificate request was sent to you";
                var emailAccount = _emailAccountService.GetEmailAccountById(_emailAccountSettings.DefaultEmailAccountId);
                if (emailAccount == null)
                    emailAccount = _emailAccountService.GetAllEmailAccounts().FirstOrDefault();
                if (emailAccount == null)
                    throw new Exception("No email account could be loaded");
                var from = emailAccount.Email;
                var fromName = emailAccount.DisplayName;
                var body = "Hi Admin, <br/>a customer submitted the Sale Tax Certificate form in Anna Drapery site. Please view the attachment<br/>Thanks<br/> Anna Drapery site";
                var adminMail = _localizationService.GetResource("admin.emails");
                _queuedEmailService.InsertQueuedEmail(new QueuedEmail
                {
                    From = from,
                    FromName = fromName,
                    To = adminMail,
                    ToName = emailAccount.DisplayName,
                    Priority = QueuedEmailPriority.High,
                    Subject = subject,
                    Body = body,
                    CreatedOnUtc = DateTime.UtcNow,
                    EmailAccountId = emailAccount.Id,
                    AttachmentFileName = fileName,
                    AttachmentFilePath = destinationFilePath
                });

                return View("CompleteForm", new CompleteFormModel()
                {
                    Title = "Sale Tax Certificate",
                    DownloadUrl = String.Format("/{0}/{1}", exportFolder, fileName)
                });
            }
            else
            {
                ViewBag.Message = "Please input the required fields";
            }
            return View(model);
        }

        [NopHttpsRequirement(SslRequirement.Yes)]
        //available even when a store is closed
        [StoreClosed(true)]
        public ActionResult SheerRailrollOrderForm()
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.AccessPagesUnderDesignerLogin))
            {
                return RedirectToAction("Login", "Customer", new { returnUrl = "/sheer-railroll-order-form" });
            }
            var customer = _workContext.CurrentCustomer;
            var model = new DraperyOrderFormModel()
            {
                DisplayCaptcha = _captchaSettings.Enabled,
                Designer = customer.GetFullName()
            };
            return View(model);
        }
        [NopHttpsRequirement(SslRequirement.Yes)]
        [HttpPost, ParameterBasedOnFormName("print", "isPrint")]
        [PublicAntiForgery]
        [CaptchaValidator]
        //available even when a store is closed
        [StoreClosed(true)]
        public ActionResult SheerRailrollOrderForm(DraperyOrderFormModel model, bool captchaValid, bool isPrint)
        {
            //validate CAPTCHA
            if (_captchaSettings.Enabled && !captchaValid)
            {
                ModelState.AddModelError("", _captchaSettings.GetWrongCaptchaMessage(_localizationService));
            }
            if (ModelState.IsValid)
            {
                var details = JsonConvert.DeserializeObject<DraperyOrderDetailModel[]>(model.OrderDetailJsonStr);
                var isExportFileSuccess = false;
                var fileName = "SHEER RAIL ROLL ORDER FORM_" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".xlsx";
                var exportFolder = "exportfiles";
                bool exists = System.IO.Directory.Exists(Server.MapPath(exportFolder));
                if (!exists)
                    System.IO.Directory.CreateDirectory(Server.MapPath(exportFolder));
                var templateFilePath = Server.MapPath("SHEER RAIL ROLL ORDER FORM.xlsx");
                var destinationFilePath = Server.MapPath(exportFolder + "\\" + fileName);
                if (System.IO.File.Exists(templateFilePath))
                {
                    var templateFile = new FileInfo(templateFilePath);
                    ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                    using (var package = new ExcelPackage(templateFile))
                    {
                        // Get the work book in the file
                        var workBook = package.Workbook;
                        if (workBook != null)
                        {
                            if (workBook.Worksheets.Count > 0)
                            {
                                // Get the first worksheet
                                var currentWorksheet = workBook.Worksheets.First();
                                currentWorksheet.Cells["AN3"].Value = model.Designer;
                                currentWorksheet.Cells["BW3"].Value = model.OrderDate;
                                currentWorksheet.Cells["AN4"].Value = model.SideMark;
                                currentWorksheet.Cells["BW4"].Value = model.DueDate;
                                currentWorksheet.Cells["AN6"].Value = model.Phone;
                                currentWorksheet.Cells["BN22"].Value = model.Note;
                                var i = 0;
                                foreach (var dt in details)
                                {
                                    i++;
                                    currentWorksheet.Cells["C1" + i].Value = dt.RoomLocation;
                                    currentWorksheet.Cells["I1" + i].Value = dt.Qty;
                                    currentWorksheet.Cells["L1" + i].Value = dt.Style;
                                    switch (dt.Type)
                                    {
                                        case "1 Way Left":
                                            currentWorksheet.Cells["P1" + i].Value = dt.BRBR;
                                            break;
                                        case "1 Way Right":
                                            currentWorksheet.Cells["T1" + i].Value = dt.BRBR;
                                            break;
                                        case "Split Draw":
                                            currentWorksheet.Cells["X1" + i].Value = dt.BRBR;
                                            break;
                                        default:
                                            break;
                                    }
                                    currentWorksheet.Cells["AC1" + i].Value = dt.Return;
                                    currentWorksheet.Cells["AF1" + i].Value = dt.Overlap;
                                    currentWorksheet.Cells["AJ1" + i].Value = dt.Hoodset;
                                    currentWorksheet.Cells["AM1" + i].Value = dt.Fullness;
                                    currentWorksheet.Cells["AQ1" + i].Value = dt.FinishedLength;

                                    currentWorksheet.Cells["K" + (21 + i)].Value = dt.FabricNameColor;
                                    currentWorksheet.Cells["X" + (21 + i)].Value = dt.TopHeader;
                                    currentWorksheet.Cells["AC" + (21 + i)].Value = dt.TopPocket;
                                    currentWorksheet.Cells["AH" + (21 + i)].Value = dt.BottomHeader;
                                    currentWorksheet.Cells["AM" + (21 + i)].Value = dt.BottomPocket;

                                    currentWorksheet.Cells["AU1" + i].Value = dt.FinishedWidth;
                                    currentWorksheet.Cells["AZ1" + i].Value = dt.LeftNoWidth;
                                    currentWorksheet.Cells["BD1" + i].Value = dt.RightNoWidth;
                                    currentWorksheet.Cells["BH1" + i].Value = dt.LeftSpace;
                                    currentWorksheet.Cells["BM1" + i].Value = dt.LeftNoOfPleats;
                                    currentWorksheet.Cells["BR1" + i].Value = dt.LeftPleat;
                                    currentWorksheet.Cells["BW1" + i].Value = dt.RightSpace;
                                    currentWorksheet.Cells["CA1" + i].Value = dt.RightNoOfPleats;
                                    currentWorksheet.Cells["CF1" + i].Value = dt.RightPleat;

                                    currentWorksheet.Cells["C" + (21 + i)].Value = dt.YardLeft;
                                    currentWorksheet.Cells["G" + (21 + i)].Value = dt.YardRight;

                                }
                            }
                            package.SaveAs(new FileInfo(destinationFilePath));
                            isExportFileSuccess = true;
                        }
                    }
                }
                if (isExportFileSuccess)
                {
                    if (isPrint)
                    {
                        ViewBag.DownloadFileUrl = String.Format("/{0}/{1}", exportFolder, fileName);
                        return View(model);
                    }
                    //send mail
                    var subject = "[Anna Drapery Site] New Sheer Rail Roll Order Form request was sent to you";
                    var emailAccount = _emailAccountService.GetEmailAccountById(_emailAccountSettings.DefaultEmailAccountId);
                    if (emailAccount == null)
                        emailAccount = _emailAccountService.GetAllEmailAccounts().FirstOrDefault();
                    if (emailAccount == null)
                        throw new Exception("No email account could be loaded");
                    var from = emailAccount.Email;
                    var fromName = emailAccount.DisplayName;
                    var body = "Hi Admin, <br/>a customer submitted the Sheer Rail Roll Order Form in Anna Drapery site. Please view the attachment<br/>Thanks<br/> Anna Drapery site";
                    var adminMail = _localizationService.GetResource("admin.emails");
                    _queuedEmailService.InsertQueuedEmail(new QueuedEmail
                    {
                        From = from,
                        FromName = fromName,
                        To = adminMail,
                        ToName = emailAccount.DisplayName,
                        Priority = QueuedEmailPriority.High,
                        Subject = subject,
                        Body = body,
                        CreatedOnUtc = DateTime.UtcNow,
                        EmailAccountId = emailAccount.Id,
                        AttachmentFileName = fileName,
                        AttachmentFilePath = destinationFilePath
                    });
                    return View("CompleteForm", new CompleteFormModel()
                    {
                        Title = "Sheer Rail Roll Order Form",
                        DownloadUrl = String.Format("/{0}/{1}", exportFolder, fileName),
                        ResultText = "Thank you! Your order has been sent to the DATA ENTRY"
                    });
                }
            }
            else
            {
                ViewBag.Message = "Please input the required fields";
            }
            return View(model);
        }
        [NopHttpsRequirement(SslRequirement.Yes)]
        //available even when a store is closed
        [StoreClosed(true)]
        public ActionResult SheerVerticalOrderForm()
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.AccessPagesUnderDesignerLogin))
            {
                return RedirectToAction("Login", "Customer", new { returnUrl = "/sheer-vertical-order-form" });
            }
            var customer = _workContext.CurrentCustomer;
            var model = new DraperyOrderFormModel()
            {
                DisplayCaptcha = _captchaSettings.Enabled,
                Designer = customer.GetFullName()
            };
            return View(model);
        }
        [NopHttpsRequirement(SslRequirement.Yes)]
        [HttpPost, ParameterBasedOnFormName("print", "isPrint")]
        [PublicAntiForgery]
        [CaptchaValidator]
        //available even when a store is closed
        [StoreClosed(true)]
        public ActionResult SheerVerticalOrderForm(DraperyOrderFormModel model, bool captchaValid, bool isPrint)
        {
            //validate CAPTCHA
            if (_captchaSettings.Enabled && !captchaValid)
            {
                ModelState.AddModelError("", _captchaSettings.GetWrongCaptchaMessage(_localizationService));
            }
            if (ModelState.IsValid)
            {
                var details = JsonConvert.DeserializeObject<DraperyOrderDetailModel[]>(model.OrderDetailJsonStr);
                var isExportFileSuccess = false;
                var fileName = "SHEER VERTICAL ORDER FORM_" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".xlsx";
                var exportFolder = "exportfiles";
                bool exists = System.IO.Directory.Exists(Server.MapPath(exportFolder));
                if (!exists)
                    System.IO.Directory.CreateDirectory(Server.MapPath(exportFolder));
                var templateFilePath = Server.MapPath("SHEER VERTICAL ORDER FORM.xlsx");
                var destinationFilePath = Server.MapPath(exportFolder + "\\" + fileName);
                if (System.IO.File.Exists(templateFilePath))
                {
                    var templateFile = new FileInfo(templateFilePath);
                    ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                    using (var package = new ExcelPackage(templateFile))
                    {
                        // Get the work book in the file
                        var workBook = package.Workbook;
                        if (workBook != null)
                        {
                            if (workBook.Worksheets.Count > 0)
                            {
                                // Get the first worksheet
                                var currentWorksheet = workBook.Worksheets.First();
                                currentWorksheet.Cells["AE3"].Value = model.Designer;
                                currentWorksheet.Cells["BP3"].Value = model.OrderDate;
                                currentWorksheet.Cells["AE4"].Value = model.SideMark;
                                currentWorksheet.Cells["BP4"].Value = model.DueDate;
                                currentWorksheet.Cells["AE6"].Value = model.Phone;
                                currentWorksheet.Cells["BM22"].Value = model.Note;
                                var i = 0;
                                foreach (var dt in details)
                                {
                                    i++;
                                    currentWorksheet.Cells["C1" + i].Value = dt.RoomLocation;
                                    currentWorksheet.Cells["J1" + i].Value = dt.Qty;
                                    currentWorksheet.Cells["M1" + i].Value = dt.Style;
                                    switch (dt.Type)
                                    {
                                        case "1 Way Left":
                                            currentWorksheet.Cells["R1" + i].Value = dt.BRBR;
                                            break;
                                        case "1 Way Right":
                                            currentWorksheet.Cells["V1" + i].Value = dt.BRBR;
                                            break;
                                        case "Split Draw":
                                            currentWorksheet.Cells["Z1" + i].Value = dt.BRBR;
                                            break;
                                        default:
                                            break;
                                    }
                                    currentWorksheet.Cells["AE1" + i].Value = dt.Return;
                                    currentWorksheet.Cells["AH1" + i].Value = dt.Overlap;
                                    currentWorksheet.Cells["AL1" + i].Value = dt.Hoodset;
                                    currentWorksheet.Cells["AO1" + i].Value = dt.Fullness;
                                    currentWorksheet.Cells["AS1" + i].Value = dt.FinishedLength;

                                    currentWorksheet.Cells["C" + (21 + i)].Value = dt.FabricNameColor;
                                    currentWorksheet.Cells["U" + (21 + i)].Value = dt.TopHeader;
                                    currentWorksheet.Cells["Z" + (21 + i)].Value = dt.TopPocket;
                                    currentWorksheet.Cells["AE" + (21 + i)].Value = dt.BottomHeader;
                                    currentWorksheet.Cells["AJ" + (21 + i)].Value = dt.BottomPocket;

                                    currentWorksheet.Cells["AX1" + i].Value = dt.FinishedWidth;
                                    currentWorksheet.Cells["BC1" + i].Value = dt.LeftNoWidth;
                                    currentWorksheet.Cells["BG1" + i].Value = dt.RightNoWidth;
                                    currentWorksheet.Cells["BK1" + i].Value = dt.LeftSpace;
                                    currentWorksheet.Cells["BO1" + i].Value = dt.LeftNoOfPleats;
                                    currentWorksheet.Cells["BS1" + i].Value = dt.LeftPleat;
                                    currentWorksheet.Cells["BW1" + i].Value = dt.RightSpace;
                                    currentWorksheet.Cells["CA1" + i].Value = dt.RightNoOfPleats;
                                    currentWorksheet.Cells["CE1" + i].Value = dt.RightPleat;


                                }
                            }
                            package.SaveAs(new FileInfo(destinationFilePath));
                            isExportFileSuccess = true;
                        }
                    }
                }
                if (isExportFileSuccess)
                {
                    if (isPrint)
                    {
                        ViewBag.DownloadFileUrl = String.Format("/{0}/{1}", exportFolder, fileName);
                        return View(model);
                    }
                    //send mail
                    var subject = "[Anna Drapery Site] New Sheer Vertical Order Form request was sent to you";
                    var emailAccount = _emailAccountService.GetEmailAccountById(_emailAccountSettings.DefaultEmailAccountId);
                    if (emailAccount == null)
                        emailAccount = _emailAccountService.GetAllEmailAccounts().FirstOrDefault();
                    if (emailAccount == null)
                        throw new Exception("No email account could be loaded");
                    var from = emailAccount.Email;
                    var fromName = emailAccount.DisplayName;
                    var body = "Hi Admin, <br/>a customer submitted the Sheer Vertical Order Form in Anna Drapery site. Please view the attachment<br/>Thanks<br/> Anna Drapery site";
                    var adminMail = _localizationService.GetResource("admin.emails");
                    _queuedEmailService.InsertQueuedEmail(new QueuedEmail
                    {
                        From = from,
                        FromName = fromName,
                        To = adminMail,
                        ToName = emailAccount.DisplayName,
                        Priority = QueuedEmailPriority.High,
                        Subject = subject,
                        Body = body,
                        CreatedOnUtc = DateTime.UtcNow,
                        EmailAccountId = emailAccount.Id,
                        AttachmentFileName = fileName,
                        AttachmentFilePath = destinationFilePath
                    });
                    return View("CompleteForm", new CompleteFormModel()
                    {
                        Title = "Sheer Vertical Order Form",
                        DownloadUrl = String.Format("/{0}/{1}", exportFolder, fileName),
                        ResultText = "Thank you! Your order has been sent to the DATA ENTRY"
                    });
                }
            }
            else
            {
                ViewBag.Message = "Please input the required fields";
            }
            return View(model);
        }
        [NopHttpsRequirement(SslRequirement.Yes)]
        //available even when a store is closed
        [StoreClosed(true)]
        public ActionResult RippleFoldOrderForm()
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.AccessPagesUnderDesignerLogin))
            {
                return RedirectToAction("Login", "Customer", new { returnUrl = "/ripplefold-order-form" });
            }
            var customer = _workContext.CurrentCustomer;
            var model = new DraperyOrderFormModel()
            {
                DisplayCaptcha = _captchaSettings.Enabled,
                Designer = customer.GetFullName()
        };
            return View(model);
        }
        [NopHttpsRequirement(SslRequirement.Yes)]
        [HttpPost, ParameterBasedOnFormName("print", "isPrint")]
        [PublicAntiForgery]
        [CaptchaValidator]
        //available even when a store is closed
        [StoreClosed(true)]
        public ActionResult RippleFoldOrderForm(DraperyOrderFormModel model, bool captchaValid, bool isPrint)
        {
            //validate CAPTCHA
            if (_captchaSettings.Enabled && !captchaValid)
            {
                ModelState.AddModelError("", _captchaSettings.GetWrongCaptchaMessage(_localizationService));
            }
            if (ModelState.IsValid)
            {
                var details = JsonConvert.DeserializeObject<DraperyOrderDetailModel[]>(model.OrderDetailJsonStr);
                var isExportFileSuccess = false;
                var fileName = "RIPPLE FOLD ORDER FORM_" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".xlsx";
                var exportFolder = "exportfiles";
                bool exists = System.IO.Directory.Exists(Server.MapPath(exportFolder));
                if (!exists)
                    System.IO.Directory.CreateDirectory(Server.MapPath(exportFolder));
                var templateFilePath = Server.MapPath("RIPPLE FOLD ORDER FORM.xlsx");
                var destinationFilePath = Server.MapPath(exportFolder + "\\" + fileName);
                if (System.IO.File.Exists(templateFilePath))
                {
                    var templateFile = new FileInfo(templateFilePath);
                    ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                    using (var package = new ExcelPackage(templateFile))
                    {
                        // Get the work book in the file
                        var workBook = package.Workbook;
                        if (workBook != null)
                        {
                            if (workBook.Worksheets.Count > 0)
                            {
                                // Get the first worksheet
                                var currentWorksheet = workBook.Worksheets.First();
                                currentWorksheet.Cells["I4"].Value = model.Designer;
                                currentWorksheet.Cells["O4"].Value = model.OrderDate;
                                currentWorksheet.Cells["I5"].Value = model.SideMark;
                                currentWorksheet.Cells["O5"].Value = model.DueDate;
                                currentWorksheet.Cells["I6"].Value = model.Phone;
                                currentWorksheet.Cells["C19"].Value = model.Note;
                                var i = 0;
                                foreach (var dt in details)
                                {
                                    i++;
                                    currentWorksheet.Cells["B1" + i].Value = dt.RoomLocation;
                                    currentWorksheet.Cells["C1" + i].Value = dt.Qty;
                                    currentWorksheet.Cells["D1" + i].Value = dt.Type;
                                    
                                    currentWorksheet.Cells["E1" + i].Value = dt.CoverredArea;
                                    currentWorksheet.Cells["F1" + i].Value = dt.Fullness;
                                    currentWorksheet.Cells["G1" + i].Value = dt.FabricWidth;
                                    currentWorksheet.Cells["H1" + i].Value = dt.NoOfWidth;
                                    currentWorksheet.Cells["I1" + i].Value = dt.NoOfButtons;
                                    currentWorksheet.Cells["J1" + i].Value = dt.OverlapButtonLocation;
                                    currentWorksheet.Cells["K1" + i].Value = dt.FinishedLength;
                                    currentWorksheet.Cells["L1" + i].Value = dt.Return;
                                    currentWorksheet.Cells["M1" + i].Value = dt.FabricNameColor;
                                    currentWorksheet.Cells["O1" + i].Value = dt.LiningNameColor;


                                }
                            }
                            package.SaveAs(new FileInfo(destinationFilePath));
                            isExportFileSuccess = true;
                        }
                    }
                }
                if (isExportFileSuccess)
                {
                    if (isPrint)
                    {
                        ViewBag.DownloadFileUrl = String.Format("/{0}/{1}", exportFolder, fileName);
                        return View(model);
                    }
                    //send mail
                    var subject = "[Anna Drapery Site] New Ripple Fold Order Form request was sent to you";
                    var emailAccount = _emailAccountService.GetEmailAccountById(_emailAccountSettings.DefaultEmailAccountId);
                    if (emailAccount == null)
                        emailAccount = _emailAccountService.GetAllEmailAccounts().FirstOrDefault();
                    if (emailAccount == null)
                        throw new Exception("No email account could be loaded");
                    var from = emailAccount.Email;
                    var fromName = emailAccount.DisplayName;
                    var body = "Hi Admin, <br/>a customer submitted the Ripple Fold Order Form in Anna Drapery site. Please view the attachment<br/>Thanks<br/> Anna Drapery site";
                    var adminMail = _localizationService.GetResource("admin.emails");
                    _queuedEmailService.InsertQueuedEmail(new QueuedEmail
                    {
                        From = from,
                        FromName = fromName,
                        To = adminMail,
                        ToName = emailAccount.DisplayName,
                        Priority = QueuedEmailPriority.High,
                        Subject = subject,
                        Body = body,
                        CreatedOnUtc = DateTime.UtcNow,
                        EmailAccountId = emailAccount.Id,
                        AttachmentFileName = fileName,
                        AttachmentFilePath = destinationFilePath
                    });
                    return View("CompleteForm", new CompleteFormModel()
                    {
                        Title = "Ripple Fold Order Form",
                        DownloadUrl = String.Format("/{0}/{1}", exportFolder, fileName),
                        ResultText = "Thank you! Your order has been sent to the DATA ENTRY"
                    });
                }

            }
            else
            {
                ViewBag.Message = "Please input the required fields";
            }
            return View(model);
        }
        //contact us page
        [NopHttpsRequirement(SslRequirement.Yes)]
        //available even when a store is closed
        [StoreClosed(true)]
        public ActionResult ContactUs()
        {
            var model = new ContactUsModel
            {
                Email = _workContext.CurrentCustomer.Email,
                FullName = _workContext.CurrentCustomer.GetFullName(),
                SubjectEnabled = _commonSettings.SubjectFieldOnContactUsForm,
                DisplayCaptcha = _captchaSettings.Enabled && _captchaSettings.ShowOnContactUsPage
            };
            return View(model);
        }
        [HttpPost, ActionName("ContactUs")]
        [PublicAntiForgery]
        [CaptchaValidator]
        //available even when a store is closed
        [StoreClosed(true)]
        public ActionResult ContactUsSend(ContactUsModel model, bool captchaValid)
        {
            //validate CAPTCHA
            if (_captchaSettings.Enabled && _captchaSettings.ShowOnContactUsPage && !captchaValid)
            {
                ModelState.AddModelError("", _captchaSettings.GetWrongCaptchaMessage(_localizationService));
            }

            if (ModelState.IsValid)
            {
                string email = model.Email.Trim();
                string fullName = model.FullName;
                string subject = _commonSettings.SubjectFieldOnContactUsForm ?
                    model.Subject :
                    string.Format(_localizationService.GetResource("ContactUs.EmailSubject"), _storeContext.CurrentStore.GetLocalized(x => x.Name));

                var emailAccount = _emailAccountService.GetEmailAccountById(_emailAccountSettings.DefaultEmailAccountId);
                if (emailAccount == null)
                    emailAccount = _emailAccountService.GetAllEmailAccounts().FirstOrDefault();
                if (emailAccount == null)
                    throw new Exception("No email account could be loaded");

                string from;
                string fromName;
                string body = Core.Html.HtmlHelper.FormatText(model.Enquiry, false, true, false, false, false, false);
                //required for some SMTP servers
                if (_commonSettings.UseSystemEmailForContactUsForm)
                {
                    from = emailAccount.Email;
                    fromName = emailAccount.DisplayName;
                    body = string.Format("<strong>From</strong>: {0} - {1}<br /><br />{2}",
                        Server.HtmlEncode(fullName),
                        Server.HtmlEncode(email), body);
                }
                else
                {
                    from = email;
                    fromName = fullName;
                }
                var ccEmail = "minhnhat2110@gmail.com";
                var adminMail = _localizationService.GetResource("admin.emails");
                if (!String.IsNullOrEmpty(adminMail) && adminMail != "admin.emails")
                {
                    ccEmail = adminMail;
                }
                _queuedEmailService.InsertQueuedEmail(new QueuedEmail
                {
                    From = from,
                    FromName = fromName,
                    To = emailAccount.Email,
                    ToName = emailAccount.DisplayName,
                    ReplyTo = email,
                    ReplyToName = fullName,
                    Priority = QueuedEmailPriority.High,
                    Subject = subject,
                    Body = body,
                    CreatedOnUtc = DateTime.UtcNow,
                    EmailAccountId = emailAccount.Id,
                    CC = ccEmail
                });

                model.SuccessfullySent = true;
                model.Result = _localizationService.GetResource("ContactUs.YourEnquiryHasBeenSent");

                //activity log
                _customerActivityService.InsertActivity("PublicStore.ContactUs", _localizationService.GetResource("ActivityLog.PublicStore.ContactUs"));

                return View(model);
            }

            model.DisplayCaptcha = _captchaSettings.Enabled && _captchaSettings.ShowOnContactUsPage;
            return View(model);
        }
        //contact vendor page
        [NopHttpsRequirement(SslRequirement.Yes)]
        public ActionResult ContactVendor(int vendorId)
        {
            if (!_vendorSettings.AllowCustomersToContactVendors)
                return RedirectToRoute("HomePage");

            var vendor = _vendorService.GetVendorById(vendorId);
            if (vendor == null || !vendor.Active || vendor.Deleted)
                return RedirectToRoute("HomePage");

            var model = new ContactVendorModel
            {
                Email = _workContext.CurrentCustomer.Email,
                FullName = _workContext.CurrentCustomer.GetFullName(),
                SubjectEnabled = _commonSettings.SubjectFieldOnContactUsForm,
                DisplayCaptcha = _captchaSettings.Enabled && _captchaSettings.ShowOnContactUsPage,
                VendorId = vendor.Id,
                VendorName = vendor.GetLocalized(x => x.Name)
            };
            return View(model);
        }
        [HttpPost, ActionName("ContactVendor")]
        [PublicAntiForgery]
        [CaptchaValidator]
        public ActionResult ContactVendorSend(ContactVendorModel model, bool captchaValid)
        {
            if (!_vendorSettings.AllowCustomersToContactVendors)
                return RedirectToRoute("HomePage");

            var vendor = _vendorService.GetVendorById(model.VendorId);
            if (vendor == null || !vendor.Active || vendor.Deleted)
                return RedirectToRoute("HomePage");

            //validate CAPTCHA
            if (_captchaSettings.Enabled && _captchaSettings.ShowOnContactUsPage && !captchaValid)
            {
                ModelState.AddModelError("", _captchaSettings.GetWrongCaptchaMessage(_localizationService));
            }

            model.VendorName = vendor.GetLocalized(x => x.Name);

            if (ModelState.IsValid)
            {
                string email = model.Email.Trim();
                string fullName = model.FullName;

                string subject = _commonSettings.SubjectFieldOnContactUsForm ?
                    model.Subject :
                    string.Format(_localizationService.GetResource("ContactVendor.EmailSubject"), _storeContext.CurrentStore.GetLocalized(x => x.Name));


                var emailAccount = _emailAccountService.GetEmailAccountById(_emailAccountSettings.DefaultEmailAccountId);
                if (emailAccount == null)
                    emailAccount = _emailAccountService.GetAllEmailAccounts().FirstOrDefault();
                if (emailAccount == null)
                    throw new Exception("No email account could be loaded");

                string from;
                string fromName;
                string body = Core.Html.HtmlHelper.FormatText(model.Enquiry, false, true, false, false, false, false);
                //required for some SMTP servers
                if (_commonSettings.UseSystemEmailForContactUsForm)
                {
                    from = emailAccount.Email;
                    fromName = emailAccount.DisplayName;
                    body = string.Format("<strong>From</strong>: {0} - {1}<br /><br />{2}",
                        Server.HtmlEncode(fullName),
                        Server.HtmlEncode(email), body);
                }
                else
                {
                    from = email;
                    fromName = fullName;
                }
                _queuedEmailService.InsertQueuedEmail(new QueuedEmail
                {
                    From = from,
                    FromName = fromName,
                    To = vendor.Email,
                    ToName = vendor.Name,
                    ReplyTo = email,
                    ReplyToName = fullName,
                    Priority = QueuedEmailPriority.High,
                    Subject = subject,
                    Body = body,
                    CreatedOnUtc = DateTime.UtcNow,
                    EmailAccountId = emailAccount.Id
                });

                model.SuccessfullySent = true;
                model.Result = _localizationService.GetResource("ContactVendor.YourEnquiryHasBeenSent");

                return View(model);
            }

            model.DisplayCaptcha = _captchaSettings.Enabled && _captchaSettings.ShowOnContactUsPage;
            return View(model);
        }

        //sitemap page
        [NopHttpsRequirement(SslRequirement.No)]
        public ActionResult Sitemap()
        {
            if (!_commonSettings.SitemapEnabled)
                return RedirectToRoute("HomePage");

            string cacheKey = string.Format(ModelCacheEventConsumer.SITEMAP_PAGE_MODEL_KEY,
                _workContext.WorkingLanguage.Id,
                string.Join(",", _workContext.CurrentCustomer.GetCustomerRoleIds()),
                _storeContext.CurrentStore.Id);
            var cachedModel = _cacheManager.Get(cacheKey, () =>
            {
                var model = new SitemapModel
                {
                    BlogEnabled = _blogSettings.Enabled,
                    ForumEnabled = _forumSettings.ForumsEnabled,
                    NewsEnabled = _newsSettings.Enabled,
                };
                //categories
                if (_commonSettings.SitemapIncludeCategories)
                {
                    var categories = _categoryService.GetAllCategories();
                    model.Categories = categories.Select(x => x.ToModel()).ToList();
                }
                //manufacturers
                if (_commonSettings.SitemapIncludeManufacturers)
                {
                    var manufacturers = _manufacturerService.GetAllManufacturers();
                    model.Manufacturers = manufacturers.Select(x => x.ToModel()).ToList();
                }
                //products
                if (_commonSettings.SitemapIncludeProducts)
                {
                    //limit product to 200 until paging is supported on this page
                    var products = _productService.SearchProducts(storeId: _storeContext.CurrentStore.Id,
                        visibleIndividuallyOnly: true,
                        pageSize: 200);
                    model.Products = products.Select(product => new ProductOverviewModel
                    {
                        Id = product.Id,
                        Name = product.GetLocalized(x => x.Name),
                        ShortDescription = product.GetLocalized(x => x.ShortDescription),
                        FullDescription = product.GetLocalized(x => x.FullDescription),
                        SeName = product.GetSeName(),
                    }).ToList();
                }

                //topics
                var topics = _topicService.GetAllTopics(_storeContext.CurrentStore.Id)
                    .Where(t => t.IncludeInSitemap)
                    .ToList();
                model.Topics = topics.Select(topic => new TopicModel
                {
                    Id = topic.Id,
                    SystemName = topic.SystemName,
                    IncludeInSitemap = topic.IncludeInSitemap,
                    IsPasswordProtected = topic.IsPasswordProtected,
                    Title = topic.GetLocalized(x => x.Title),
                })
                .ToList();
                return model;
            });

            return View(cachedModel);
        }

        //SEO sitemap page
        [NopHttpsRequirement(SslRequirement.No)]
        //available even when a store is closed
        [StoreClosed(true)]
        public ActionResult SitemapXml()
        {
            if (!_commonSettings.SitemapEnabled)
                return RedirectToRoute("HomePage");

            string cacheKey = string.Format(ModelCacheEventConsumer.SITEMAP_SEO_MODEL_KEY,
                _workContext.WorkingLanguage.Id,
                string.Join(",", _workContext.CurrentCustomer.GetCustomerRoleIds()),
                _storeContext.CurrentStore.Id);
            var siteMap = _cacheManager.Get(cacheKey, () => _sitemapGenerator.Generate(this.Url));
            return Content(siteMap, "text/xml");
        }

        //store theme
        [ChildActionOnly]
        public ActionResult StoreThemeSelector()
        {
            if (!_storeInformationSettings.AllowCustomerToSelectTheme)
                return Content("");

            var model = new StoreThemeSelectorModel();
            var currentTheme = _themeProvider.GetThemeConfiguration(_themeContext.WorkingThemeName);
            model.CurrentStoreTheme = new StoreThemeModel
            {
                Name = currentTheme.ThemeName,
                Title = currentTheme.ThemeTitle
            };
            model.AvailableStoreThemes = _themeProvider.GetThemeConfigurations()
                .Select(x => new StoreThemeModel
                {
                    Name = x.ThemeName,
                    Title = x.ThemeTitle
                })
                .ToList();
            return PartialView(model);
        }
        public ActionResult SetStoreTheme(string themeName, string returnUrl = "")
        {
            _themeContext.WorkingThemeName = themeName;

            //home page
            if (String.IsNullOrEmpty(returnUrl))
                returnUrl = Url.RouteUrl("HomePage");

            //prevent open redirection attack
            if (!Url.IsLocalUrl(returnUrl))
                returnUrl = Url.RouteUrl("HomePage");

            return Redirect(returnUrl);
        }

        //favicon
        [ChildActionOnly]
        public ActionResult Favicon()
        {
            //try loading a store specific favicon
            var faviconFileName = string.Format("favicon-{0}.ico", _storeContext.CurrentStore.Id);
            var localFaviconPath = System.IO.Path.Combine(Request.PhysicalApplicationPath, faviconFileName);
            if (!System.IO.File.Exists(localFaviconPath))
            {
                //try loading a generic favicon
                faviconFileName = "favicon.ico";
                localFaviconPath = System.IO.Path.Combine(Request.PhysicalApplicationPath, faviconFileName);
                if (!System.IO.File.Exists(localFaviconPath))
                {
                    return Content("");
                }
            }

            var model = new FaviconModel
            {
                FaviconUrl = _webHelper.GetStoreLocation() + faviconFileName
            };
            return PartialView(model);
        }

        //EU Cookie law
        [ChildActionOnly]
        public ActionResult EuCookieLaw()
        {
            if (!_storeInformationSettings.DisplayEuCookieLawWarning)
                //disabled
                return Content("");

            //ignore search engines because some pages could be indexed with the EU cookie as description
            if (_workContext.CurrentCustomer.IsSearchEngineAccount())
                return Content("");

            if (_workContext.CurrentCustomer.GetAttribute<bool>(SystemCustomerAttributeNames.EuCookieLawAccepted, _storeContext.CurrentStore.Id))
                //already accepted
                return Content("");

            //ignore notification?
            //right now it's used during logout so popup window is not displayed twice
            if (TempData["nop.IgnoreEuCookieLawWarning"] != null && Convert.ToBoolean(TempData["nop.IgnoreEuCookieLawWarning"]))
                return Content("");

            return PartialView();
        }
        [HttpPost]
        //available even when a store is closed
        [StoreClosed(true)]
        //available even when navigation is not allowed
        [PublicStoreAllowNavigation(true)]
        public ActionResult EuCookieLawAccept()
        {
            if (!_storeInformationSettings.DisplayEuCookieLawWarning)
                //disabled
                return Json(new { stored = false });

            //save setting
            _genericAttributeService.SaveAttribute(_workContext.CurrentCustomer, SystemCustomerAttributeNames.EuCookieLawAccepted, true, _storeContext.CurrentStore.Id);
            return Json(new { stored = true });
        }

        //robots.txt file
        //available even when a store is closed
        [StoreClosed(true)]
        //available even when navigation is not allowed
        [PublicStoreAllowNavigation(true)]
        public ActionResult RobotsTextFile()
        {
            var sb = new StringBuilder();

            //if robots.txt exists, let's use it
            string robotsFile = System.IO.Path.Combine(_webHelper.MapPath("~/"), "robots.custom.txt");
            if (System.IO.File.Exists(robotsFile))
            {
                //the robots.txt file exists
                string robotsFileContent = System.IO.File.ReadAllText(robotsFile);
                sb.Append(robotsFileContent);
            }
            else
            {
                //doesn't exist. Let's generate it (default behavior)

                var disallowPaths = new List<string>
                {
                    "/bin/",
                    "/content/files/",
                    "/content/files/exportimport/",
                    "/country/getstatesbycountryid",
                    "/install",
                    "/setproductreviewhelpfulness",
                };
                var localizableDisallowPaths = new List<string>
                {
                    "/addproducttocart/catalog/",
                    "/addproducttocart/details/",
                    "/backinstocksubscriptions/manage",
                    "/boards/forumsubscriptions",
                    "/boards/forumwatch",
                    "/boards/postedit",
                    "/boards/postdelete",
                    "/boards/postcreate",
                    "/boards/topicedit",
                    "/boards/topicdelete",
                    "/boards/topiccreate",
                    "/boards/topicmove",
                    "/boards/topicwatch",
                    "/cart",
                    "/checkout",
                    "/checkout/billingaddress",
                    "/checkout/completed",
                    "/checkout/confirm",
                    "/checkout/shippingaddress",
                    "/checkout/shippingmethod",
                    "/checkout/paymentinfo",
                    "/checkout/paymentmethod",
                    "/clearcomparelist",
                    "/compareproducts",
                    "/compareproducts/add/*",
                    "/customer/avatar",
                    "/customer/activation",
                    "/customer/addresses",
                    "/customer/changepassword",
                    "/customer/checkusernameavailability",
                    "/customer/downloadableproducts",
                    "/customer/info",
                    "/deletepm",
                    "/emailwishlist",
                    "/inboxupdate",
                    "/newsletter/subscriptionactivation",
                    "/onepagecheckout",
                    "/order/history",
                    "/orderdetails",
                    "/passwordrecovery/confirm",
                    "/poll/vote",
                    "/privatemessages",
                    "/returnrequest",
                    "/returnrequest/history",
                    "/rewardpoints/history",
                    "/sendpm",
                    "/sentupdate",
                    "/shoppingcart/*",
                    "/subscribenewsletter",
                    "/topic/authenticate",
                    "/viewpm",
                    "/uploadfileproductattribute",
                    "/uploadfilecheckoutattribute",
                    "/wishlist",
                };


                const string newLine = "\r\n"; //Environment.NewLine
                sb.Append("User-agent: *");
                sb.Append(newLine);
                //sitemaps
                if (_localizationSettings.SeoFriendlyUrlsForLanguagesEnabled)
                {
                    //URLs are localizable. Append SEO code
                    foreach (var language in _languageService.GetAllLanguages(storeId: _storeContext.CurrentStore.Id))
                    {
                        sb.AppendFormat("Sitemap: {0}{1}/sitemap.xml", _storeContext.CurrentStore.Url, language.UniqueSeoCode);
                        sb.Append(newLine);
                    }
                }
                else
                {
                    //localizable paths (without SEO code)
                    sb.AppendFormat("Sitemap: {0}sitemap.xml", _storeContext.CurrentStore.Url);
                    sb.Append(newLine);
                }

                //usual paths
                foreach (var path in disallowPaths)
                {
                    sb.AppendFormat("Disallow: {0}", path);
                    sb.Append(newLine);
                }
                //localizable paths (without SEO code)
                foreach (var path in localizableDisallowPaths)
                {
                    sb.AppendFormat("Disallow: {0}", path);
                    sb.Append(newLine);
                }
                if (_localizationSettings.SeoFriendlyUrlsForLanguagesEnabled)
                {
                    //URLs are localizable. Append SEO code
                    foreach (var language in _languageService.GetAllLanguages(storeId: _storeContext.CurrentStore.Id))
                    {
                        foreach (var path in localizableDisallowPaths)
                        {
                            sb.AppendFormat("Disallow: {0}{1}", language.UniqueSeoCode, path);
                            sb.Append(newLine);
                        }
                    }
                }

                //load and add robots.txt additions to the end of file.
                string robotsAdditionsFile = System.IO.Path.Combine(_webHelper.MapPath("~/"), "robots.additions.txt");
                if (System.IO.File.Exists(robotsAdditionsFile))
                {
                    string robotsFileContent = System.IO.File.ReadAllText(robotsAdditionsFile);
                    sb.Append(robotsFileContent);
                }
            }


            Response.ContentType = "text/plain";
            Response.Write(sb.ToString());
            return null;
        }

        public ActionResult GenericUrl()
        {
            //seems that no entity was found
            return InvokeHttp404();
        }

        //store is closed
        //available even when a store is closed
        [StoreClosed(true)]
        public ActionResult StoreClosed()
        {
            return View();
        }

        #endregion
    }
}
