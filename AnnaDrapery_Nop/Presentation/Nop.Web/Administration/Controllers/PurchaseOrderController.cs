using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Nop.Admin.Extensions;
using Nop.Admin.Models.Catalog;
using Nop.Admin.Models.Orders;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Discounts;
using Nop.Services.Catalog;
using Nop.Services.Customers;
using Nop.Services.Discounts;
using Nop.Services.ExportImport;
using Nop.Services.Helpers;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Media;
using Nop.Services.Orders;
using Nop.Services.Security;
using Nop.Services.Seo;
using Nop.Services.Stores;
using Nop.Services.Vendors;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Kendoui;
using Nop.Web.Framework.Mvc;

namespace Nop.Admin.Controllers
{
    public partial class PurchaseOrderController : BaseAdminController
    {
        #region Fields

        private readonly IBrandService _brandService;
        private readonly IProductService _productService;
        private readonly ICustomerService _customerService;
        private readonly IUrlRecordService _urlRecordService;
        private readonly IPictureService _pictureService;
        private readonly ILanguageService _languageService;
        private readonly ILocalizationService _localizationService;
        private readonly ILocalizedEntityService _localizedEntityService;
        private readonly IExportManager _exportManager;
        private readonly ICustomerActivityService _customerActivityService;
        private readonly IVendorService _vendorService;
        private readonly IPermissionService _permissionService;
        private readonly CatalogSettings _catalogSettings;
        private readonly IPurchaseOrderService _purchaseOrderService;
        private readonly IDateTimeHelper _dateTimeHelper;

        #endregion
        
        #region Constructors

        public PurchaseOrderController(IBrandService brandService, 
            IProductService productService,
            ICustomerService customerService, 
            IUrlRecordService urlRecordService, 
            IPictureService pictureService,
            ILanguageService languageService, 
            ILocalizationService localizationService,
            ILocalizedEntityService localizedEntityService, 
            IExportManager exportManager,          
            ICustomerActivityService customerActivityService, 
            IVendorService vendorService,
            IPermissionService permissionService,
            IPurchaseOrderService purchaseOrderService,
            IDateTimeHelper dateTimeHelper,
            CatalogSettings catalogSettings)
        {
            this._brandService = brandService;
            this._productService = productService;
            this._customerService = customerService;
            this._urlRecordService = urlRecordService;
            this._pictureService = pictureService;
            this._languageService = languageService;
            this._localizationService = localizationService;
            this._localizedEntityService = localizedEntityService;
            this._exportManager = exportManager;
            this._customerActivityService = customerActivityService;
            this._vendorService = vendorService;
            this._permissionService = permissionService;
            this._catalogSettings = catalogSettings;
            this._purchaseOrderService = purchaseOrderService;
            this._dateTimeHelper = dateTimeHelper;
        }

        #endregion
        
       
        #region List

        
        [HttpPost]
        public ActionResult List(DataSourceRequest command, POListModel model)
        {

            var pos = _purchaseOrderService.GetAllPurchaseOrders(model.OrderId,
                command.Page - 1, command.PageSize);
            var gridModel = new DataSourceResult
            {
                Data = pos.Select(x =>
                    {
                        var po = new PurchaseOrderModel()
                        {
                            Id = x.Id,
                            OrderId = x.OrderId,
                            PONumber = x.PONumber,
                            VendorId = x.VendorId,
                            CreatedOn = _dateTimeHelper.ConvertToUserTime(x.CreatedOnUtc, DateTimeKind.Utc),
                        };
                        var vendor = this._vendorService.GetVendorById(x.VendorId);
                        if (vendor!=null)
                        {
                            po.VendorName = vendor.Name;
                        }
                        return po;
                    }
                ),
                Total = pos.TotalCount
            };

            return Json(gridModel);
        }

        #endregion

        public ActionResult PurchaseOrderContentPopup(int id)
        {
            var model = new PurchaseOrderModel();
            var po = _purchaseOrderService.GetPOById(id);
            if (po != null)
            {
                model = po.ToModel();
            }
            else
            {
                model.Content = "Cannot find Purchase Order";
            }
            return View(model);
        }
    }
}
