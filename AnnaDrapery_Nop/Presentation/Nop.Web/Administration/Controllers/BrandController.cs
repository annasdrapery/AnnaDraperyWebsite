using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Nop.Admin.Extensions;
using Nop.Admin.Models.Catalog;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Discounts;
using Nop.Services.Catalog;
using Nop.Services.Customers;
using Nop.Services.Discounts;
using Nop.Services.ExportImport;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Media;
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
    public partial class BrandController : BaseAdminController
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
        private readonly IBrandTemplateService _brandTemplateService;
        #endregion
        
        #region Constructors

        public BrandController(IBrandService brandService, 
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
            IBrandTemplateService brandTemplateService,
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
            this._brandTemplateService = brandTemplateService;
        }

        #endregion
        
        #region Utilities

        [NonAction]
        protected virtual void UpdateLocales(Brand brand, BrandModel model)
        {
            foreach (var localized in model.Locales)
            {
                _localizedEntityService.SaveLocalizedValue(brand,
                                                               x => x.Name,
                                                               localized.Name,
                                                               localized.LanguageId);

                _localizedEntityService.SaveLocalizedValue(brand,
                                                           x => x.Description,
                                                           localized.Description,
                                                           localized.LanguageId);

                _localizedEntityService.SaveLocalizedValue(brand,
                                                           x => x.MetaKeywords,
                                                           localized.MetaKeywords,
                                                           localized.LanguageId);

                _localizedEntityService.SaveLocalizedValue(brand,
                                                           x => x.MetaDescription,
                                                           localized.MetaDescription,
                                                           localized.LanguageId);

                _localizedEntityService.SaveLocalizedValue(brand,
                                                           x => x.MetaTitle,
                                                           localized.MetaTitle,
                                                           localized.LanguageId);

                //search engine name
                var seName = brand.ValidateSeName(localized.SeName, localized.Name, false);
                _urlRecordService.SaveSlug(brand, seName, localized.LanguageId);
            }
        }

        [NonAction]
        protected virtual void UpdatePictureSeoNames(Brand brand)
        {
            var picture = _pictureService.GetPictureById(brand.PictureId);
            if (picture != null)
                _pictureService.SetSeoFilename(picture.Id, _pictureService.GetPictureSeName(brand.Name));
        }

        #endregion
        
        #region List

        public ActionResult Index()
        {
            return RedirectToAction("List");
        }

        public ActionResult List()
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageBrands))
                return AccessDeniedView();

            var model = new BrandListModel();
            return View(model);
        }

        [HttpPost]
        public ActionResult List(DataSourceRequest command, BrandListModel model)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageBrands))
                return AccessDeniedView();

            var brands = _brandService.GetAllBrands(model.SearchBrandName,
                command.Page - 1, command.PageSize, true);
            var gridModel = new DataSourceResult
            {
                Data = brands.Select(x =>
                    {
                        var brandModel = x.ToModel();
                        var defaultPicture = _pictureService.GetPictureById(x.PictureId);
                        brandModel.PictureThumbnailUrl = _pictureService.GetPictureUrl(defaultPicture, 75, true);
                        return brandModel;
                    }
                ),
                Total = brands.TotalCount
            };

            return Json(gridModel);
        }

        #endregion

        #region Create / Edit / Delete

        public ActionResult Create()
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageBrands))
                return AccessDeniedView();

            var model = new BrandModel();
           
            //default values
            model.PageSize = _catalogSettings.DefaultManufacturerPageSize;
            model.PageSizeOptions = _catalogSettings.DefaultManufacturerPageSizeOptions;
            model.Published = true;
            model.AllowCustomersToSelectPageSize = true;
            //templates
            PrepareTemplatesModel(model);
            return View(model);
        }

        [HttpPost, ParameterBasedOnFormName("save-continue", "continueEditing")]
        public ActionResult Create(BrandModel model, bool continueEditing)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageBrands))
                return AccessDeniedView();

            if (ModelState.IsValid)
            {
                var brand = model.ToEntity();
                brand.CreatedOnUtc = DateTime.UtcNow;
                brand.UpdatedOnUtc = DateTime.UtcNow;
                _brandService.InsertBrand(brand);
                //search engine name
                model.SeName = brand.ValidateSeName(model.SeName, brand.Name, true);
                _urlRecordService.SaveSlug(brand, model.SeName, 0);
                //locales
                UpdateLocales(brand, model);
                
                _brandService.UpdateBrand(brand);
                //update picture seo file name
                UpdatePictureSeoNames(brand);
               
                //activity log
                _customerActivityService.InsertActivity("AddNewBrand", _localizationService.GetResource("ActivityLog.AddNewBrand"), brand.Name);

                SuccessNotification(_localizationService.GetResource("Admin.Catalog.Brands.Added"));
                return continueEditing ? RedirectToAction("Edit", new { id = brand.Id }) : RedirectToAction("List");
            }


            return View(model);
        }

        public ActionResult Edit(int id)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageBrands))
                return AccessDeniedView();

            var brand = _brandService.GetBrandById(id);
            if (brand == null || brand.Deleted)
                //No brand found with the specified id
                return RedirectToAction("List");

            var model = brand.ToModel();
            //locales
            AddLocales(_languageService, model.Locales, (locale, languageId) =>
            {
                locale.Name = brand.GetLocalized(x => x.Name, languageId, false, false);
                locale.Description = brand.GetLocalized(x => x.Description, languageId, false, false);
                locale.MetaKeywords = brand.GetLocalized(x => x.MetaKeywords, languageId, false, false);
                locale.MetaDescription = brand.GetLocalized(x => x.MetaDescription, languageId, false, false);
                locale.MetaTitle = brand.GetLocalized(x => x.MetaTitle, languageId, false, false);
                locale.SeName = brand.GetSeName(languageId, false, false);
            });
            //templates
            PrepareTemplatesModel(model);

            return View(model);
        }

        [HttpPost, ParameterBasedOnFormName("save-continue", "continueEditing")]
        public ActionResult Edit(BrandModel model, bool continueEditing)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageBrands))
                return AccessDeniedView();

            var brand = _brandService.GetBrandById(model.Id);
            if (brand == null || brand.Deleted)
                //No brand found with the specified id
                return RedirectToAction("List");

            if (ModelState.IsValid)
            {
                int prevPictureId = brand.PictureId;
                brand = model.ToEntity(brand);
                brand.UpdatedOnUtc = DateTime.UtcNow;
                _brandService.UpdateBrand(brand);
                //search engine name
                model.SeName = brand.ValidateSeName(model.SeName, brand.Name, true);
                _urlRecordService.SaveSlug(brand, model.SeName, 0);
                //locales
                UpdateLocales(brand, model);
                
                _brandService.UpdateBrand(brand);
                //delete an old picture (if deleted or updated)
                if (prevPictureId > 0 && prevPictureId != brand.PictureId)
                {
                    var prevPicture = _pictureService.GetPictureById(prevPictureId);
                    if (prevPicture != null)
                        _pictureService.DeletePicture(prevPicture);
                }
                //update picture seo file name
                UpdatePictureSeoNames(brand);
               
                //activity log
                _customerActivityService.InsertActivity("EditBrand", _localizationService.GetResource("ActivityLog.EditBrand"), brand.Name);

                SuccessNotification(_localizationService.GetResource("Admin.Catalog.Brands.Updated"));

                if (continueEditing)
                {
                    //selected tab
                    SaveSelectedTabIndex();

                    return RedirectToAction("Edit",  new {id = brand.Id});
                }
                return RedirectToAction("List");
            }

            return View(model);
        }
        [NonAction]
        protected virtual void PrepareTemplatesModel(BrandModel model)
        {
            if (model == null)
                throw new ArgumentNullException("model");

            var templates = _brandTemplateService.GetAllBrandTemplates();
            foreach (var template in templates)
            {
                model.AvailableBrandTemplates.Add(new SelectListItem
                {
                    Text = template.Name,
                    Value = template.Id.ToString()
                });
            }
        }
        [HttpPost]
        public ActionResult Delete(int id)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageBrands))
                return AccessDeniedView();

            var brand = _brandService.GetBrandById(id);
            if (brand == null)
                //No brand found with the specified id
                return RedirectToAction("List");

            _brandService.DeleteBrand(brand);

            //activity log
            _customerActivityService.InsertActivity("DeleteBrand", _localizationService.GetResource("ActivityLog.DeleteBrand"), brand.Name);

            SuccessNotification(_localizationService.GetResource("Admin.Catalog.Brands.Deleted"));
            return RedirectToAction("List");
        }
        
        #endregion

        #region Export / Import

        public ActionResult ExportXml()
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageManufacturers))
                return AccessDeniedView();

            try
            {
                var brands = _brandService.GetAllBrands(showHidden: true);
                var xml = _exportManager.ExportBrandsToXml(brands);
                return new XmlDownloadResult(xml, "brands.xml");
            }
            catch (Exception exc)
            {
                ErrorNotification(exc);
                return RedirectToAction("List");
            }
        }

        #endregion

    }
}
