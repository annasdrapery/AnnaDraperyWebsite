using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Nop.Admin.Extensions;
using Nop.Admin.Models.Vendors;
using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Vendors;
using Nop.Services.Customers;
using Nop.Services.Helpers;
using Nop.Services.Localization;
using Nop.Services.Media;
using Nop.Services.Security;
using Nop.Services.Seo;
using Nop.Services.Vendors;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Kendoui;
using Nop.Web.Framework.Mvc;
using Nop.Web.Framework;

namespace Nop.Admin.Controllers
{
    public partial class VendorController : BaseAdminController
    {
        #region Fields

        private readonly ICustomerService _customerService;
        private readonly ILocalizationService _localizationService;
        private readonly IVendorService _vendorService;
        private readonly IPermissionService _permissionService;
        private readonly IUrlRecordService _urlRecordService;
        private readonly ILanguageService _languageService;
        private readonly ILocalizedEntityService _localizedEntityService;
        private readonly IPictureService _pictureService;
        private readonly IDateTimeHelper _dateTimeHelper;
        private readonly VendorSettings _vendorSettings;
        private readonly IPOTemplateMessageService _pOTemplateMessageService;
        private readonly IWorkContext _workContext;

        #endregion

        #region Constructors

        public VendorController(ICustomerService customerService, 
            ILocalizationService localizationService,
            IVendorService vendorService, 
            IPermissionService permissionService,
            IUrlRecordService urlRecordService,
            ILanguageService languageService,
            ILocalizedEntityService localizedEntityService,
            IPictureService pictureService,
            IDateTimeHelper dateTimeHelper,
            IPOTemplateMessageService pOTemplateMessageService,
            IWorkContext workContext,
            VendorSettings vendorSettings)
        {
            this._customerService = customerService;
            this._localizationService = localizationService;
            this._vendorService = vendorService;
            this._permissionService = permissionService;
            this._urlRecordService = urlRecordService;
            this._languageService = languageService;
            this._localizedEntityService = localizedEntityService;
            this._pictureService = pictureService;
            this._dateTimeHelper = dateTimeHelper;
            this._vendorSettings = vendorSettings;
            this._pOTemplateMessageService = pOTemplateMessageService;
            this._workContext = workContext;
        }

        #endregion

        #region Utilities

        [NonAction]
        protected virtual void UpdatePictureSeoNames(Vendor vendor)
        {
            var picture = _pictureService.GetPictureById(vendor.PictureId);
            if (picture != null)
                _pictureService.SetSeoFilename(picture.Id, _pictureService.GetPictureSeName(vendor.Name));
        }

        [NonAction]
        protected virtual void UpdateLocales(Vendor vendor, VendorModel model)
        {
            foreach (var localized in model.Locales)
            {
                _localizedEntityService.SaveLocalizedValue(vendor,
                                                               x => x.Name,
                                                               localized.Name,
                                                               localized.LanguageId);

                _localizedEntityService.SaveLocalizedValue(vendor,
                                                           x => x.Description,
                                                           localized.Description,
                                                           localized.LanguageId);

                _localizedEntityService.SaveLocalizedValue(vendor,
                                                           x => x.MetaKeywords,
                                                           localized.MetaKeywords,
                                                           localized.LanguageId);

                _localizedEntityService.SaveLocalizedValue(vendor,
                                                           x => x.MetaDescription,
                                                           localized.MetaDescription,
                                                           localized.LanguageId);

                _localizedEntityService.SaveLocalizedValue(vendor,
                                                           x => x.MetaTitle,
                                                           localized.MetaTitle,
                                                           localized.LanguageId);

                //search engine name
                var seName = vendor.ValidateSeName(localized.SeName, localized.Name, false);
                _urlRecordService.SaveSlug(vendor, seName, localized.LanguageId);
            }
        }

        #endregion

        #region Vendors

        //list
        public ActionResult Index()
        {
            return RedirectToAction("List");
        }

        public ActionResult List()
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageVendors))
                return AccessDeniedView();

            var model = new VendorListModel();
            return View(model);
        }

        [HttpPost]
        public ActionResult List(DataSourceRequest command, VendorListModel model)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageVendors))
                return AccessDeniedView();

            var vendors = _vendorService.GetAllVendors(model.SearchName, command.Page - 1, command.PageSize, true);
            var gridModel = new DataSourceResult
            {
                Data = vendors.Select(x =>
                {
                    var vendorModel = x.ToModel();
                    var defaultPicture = _pictureService.GetPictureById(x.PictureId);
                    vendorModel.PictureThumbnailUrl = _pictureService.GetPictureUrl(defaultPicture, 75, true);
                    return vendorModel;
                }),
                Total = vendors.TotalCount,
            };

            return Json(gridModel);
        }

        //create

        public ActionResult Create()
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageVendors))
                return AccessDeniedView();


            var model = new VendorModel();
            //locales
            AddLocales(_languageService, model.Locales);
            //default values
            model.PageSize = 6;
            model.Active = true;
            model.AllowCustomersToSelectPageSize = true;
            model.PageSizeOptions = _vendorSettings.DefaultVendorPageSizeOptions;

            //default value
            model.Active = true;
            return View(model);
        }

        [HttpPost, ParameterBasedOnFormName("save-continue", "continueEditing")]
        [FormValueRequired("save", "save-continue")]
        public ActionResult Create(VendorModel model, bool continueEditing)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageVendors))
                return AccessDeniedView();

            if (ModelState.IsValid)
            {
                var vendor = model.ToEntity();
                _vendorService.InsertVendor(vendor);
                //search engine name
                model.SeName = vendor.ValidateSeName(model.SeName, vendor.Name, true);
                _urlRecordService.SaveSlug(vendor, model.SeName, 0);
                //locales
                UpdateLocales(vendor, model);
                //update picture seo file name
                UpdatePictureSeoNames(vendor);

                SuccessNotification(_localizationService.GetResource("Admin.Vendors.Added"));
                return continueEditing ? RedirectToAction("Edit", new { id = vendor.Id }) : RedirectToAction("List");
            }

            //If we got this far, something failed, redisplay form
            return View(model);
        }


        //edit
        public ActionResult Edit(int id)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageVendors))
                return AccessDeniedView();

            var vendor = _vendorService.GetVendorById(id);
            if (vendor == null || vendor.Deleted)
                //No vendor found with the specified id
                return RedirectToAction("List");

            var model = vendor.ToModel();
            //locales
            AddLocales(_languageService, model.Locales, (locale, languageId) =>
            {
                locale.Name = vendor.GetLocalized(x => x.Name, languageId, false, false);
                locale.Description = vendor.GetLocalized(x => x.Description, languageId, false, false);
                locale.MetaKeywords = vendor.GetLocalized(x => x.MetaKeywords, languageId, false, false);
                locale.MetaDescription = vendor.GetLocalized(x => x.MetaDescription, languageId, false, false);
                locale.MetaTitle = vendor.GetLocalized(x => x.MetaTitle, languageId, false, false);
                locale.SeName = vendor.GetSeName(languageId, false, false);
            });
            //associated customer emails
            model.AssociatedCustomers = _customerService
                .GetAllCustomers(vendorId: vendor.Id)
                .Select(c => new VendorModel.AssociatedCustomerInfo()
                {
                    Id = c.Id,
                    Email = c.Email
                })
                .ToList();

            return View(model);
        }

        [HttpPost, ParameterBasedOnFormName("save-continue", "continueEditing")]
        public ActionResult Edit(VendorModel model, bool continueEditing)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageVendors))
                return AccessDeniedView();

            var vendor = _vendorService.GetVendorById(model.Id);
            if (vendor == null || vendor.Deleted)
                //No vendor found with the specified id
                return RedirectToAction("List");

            if (ModelState.IsValid)
            {
                int prevPictureId = vendor.PictureId;
                vendor = model.ToEntity(vendor);
                _vendorService.UpdateVendor(vendor);
                //search engine name
                model.SeName = vendor.ValidateSeName(model.SeName, vendor.Name, true);
                _urlRecordService.SaveSlug(vendor, model.SeName, 0);
                //locales
                UpdateLocales(vendor, model);
                //delete an old picture (if deleted or updated)
                if (prevPictureId > 0 && prevPictureId != vendor.PictureId)
                {
                    var prevPicture = _pictureService.GetPictureById(prevPictureId);
                    if (prevPicture != null)
                        _pictureService.DeletePicture(prevPicture);
                }
                //update picture seo file name
                UpdatePictureSeoNames(vendor);

                SuccessNotification(_localizationService.GetResource("Admin.Vendors.Updated"));
                if (continueEditing)
                {
                    //selected tab
                    SaveSelectedTabIndex();

                    return RedirectToAction("Edit",  new {id = vendor.Id});
                }
                return RedirectToAction("List");
            }

            //If we got this far, something failed, redisplay form

            //associated customer emails
            model.AssociatedCustomers = _customerService
                .GetAllCustomers(vendorId: vendor.Id)
                .Select(c => new VendorModel.AssociatedCustomerInfo()
                {
                    Id = c.Id,
                    Email = c.Email
                })
                .ToList();

            return View(model);
        }

        //delete
        [HttpPost]
        public ActionResult Delete(int id)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageVendors))
                return AccessDeniedView();

            var vendor = _vendorService.GetVendorById(id);
            if (vendor == null)
                //No vendor found with the specified id
                return RedirectToAction("List");

            //clear associated customer references
            var associatedCustomers = _customerService.GetAllCustomers(vendorId: vendor.Id);
            foreach (var customer in associatedCustomers)
            {
                customer.VendorId = 0;
                _customerService.UpdateCustomer(customer);
            }

            //delete a vendor
            _vendorService.DeleteVendor(vendor);

            SuccessNotification(_localizationService.GetResource("Admin.Vendors.Deleted"));
            return RedirectToAction("List");
        }

        #endregion

        #region PO Templates
        [HttpPost]
        public ActionResult POTemplateList(DataSourceRequest command, POTemplateListModel model)
        {

            var pts = _pOTemplateMessageService.GetAllPOTemplateMessages(model.VendorId,
                command.Page - 1, command.PageSize);
            var gridModel = new DataSourceResult
            {
                Data = pts.Select(x =>
                {
                    var pt = new POTemplateMessageModel()
                    {
                        Id = x.Id,
                        VendorId = x.VendorId,
                        Active = x.Active,
                        BCC = x.BCC,
                        CC = x.CC,
                        Name = x.Name,
                        CreatedOn = _dateTimeHelper.ConvertToUserTime(x.CreatedOnUtc, DateTimeKind.Utc),
                    };
                    if (x.POTemplate_ProductTypes.Count > 0)
                    {
                        pt.ProductTypesDisplay = String.Join("<br/>", x.POTemplate_ProductTypes.Select(t => ((ProductType)t.ProductTypeId).GetLocalizedEnum(_localizationService, _workContext)).ToArray());

                    }
                    return pt;
                }
                ),
                Total = pts.TotalCount
            };

            return Json(gridModel);
        }

        public ActionResult CreatePOTemplate()
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageVendors))
                return AccessDeniedView();
            var model = new POTemplateMessageModel();
            PreparePOTemplateMessageModel(model, null);
            model.Active = true;
            return View(model);
        }
        [HttpPost, ParameterBasedOnFormName("save-continue", "continueEditing")]
        [FormValueRequired("save", "save-continue")]
        public ActionResult CreatePOTemplate(POTemplateMessageModel model, bool continueEditing)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageVendors))
                return AccessDeniedView();

            if (ModelState.IsValid)
            {
                SavePOTemplate(model);
                SuccessNotification(_localizationService.GetResource("Admin.POTemplate.Added"));
                return continueEditing ? RedirectToAction("EditPOTemplate", new { id = model.Id }) : RedirectToAction("Edit", new { id = model.VendorId });
            }
            return View(model);
        }

        public ActionResult EditPOTemplate(int id)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageVendors))
                return AccessDeniedView();
            var poTemplate = _pOTemplateMessageService.GetById(id);
            if (poTemplate == null || poTemplate.Deleted)
                //No vendor found with the specified id
                return RedirectToAction("List");
            var model = new POTemplateMessageModel() { Id = id };
            PreparePOTemplateMessageModel(model, poTemplate);
            return View(model);
        }
        [HttpPost, ParameterBasedOnFormName("save-continue", "continueEditing")]
        public ActionResult EditPOTemplate(POTemplateMessageModel model, bool continueEditing)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageVendors))
                return AccessDeniedView();
            if (ModelState.IsValid)
            {
                SavePOTemplate(model);
                SuccessNotification(_localizationService.GetResource("Admin.POTemplate.Edited"));
                return continueEditing ? RedirectToAction("EditPOTemplate", new { id = model.Id }) : RedirectToAction("Edit", new { id = model.VendorId });
            }
            return View(model);
        }

        [NonAction]
        protected virtual void PreparePOTemplateMessageModel(POTemplateMessageModel model, POTemplateMessage poTemplate)
        {
            if (model == null)
                throw new ArgumentNullException("model");

            if (poTemplate != null)//edit
            {
                model.Name = poTemplate.Name;
                model.Subject = poTemplate.Subject;
                model.VendorId = poTemplate.VendorId;
                model.POTemplateHtml = poTemplate.POTemplateHtml;
                model.POTemplateExcel = poTemplate.POTemplateExcel;
                model.Active = poTemplate.Active;
                model.CC = poTemplate.CC;
                model.BCC = poTemplate.BCC;
                model.CreatedOn = poTemplate.CreatedOnUtc;
                model.SelectedProductTypes = poTemplate.POTemplate_ProductTypes
                .Select(x =>
                    new POTemplate_ProductTypeModel()
                    {
                        Id = x.Id,
                        ProductTypeId = x.ProductTypeId,
                        ProductTypeName = ((ProductType)x.ProductTypeId).GetLocalizedEnum(_localizationService, _workContext)
                    }
                    ).ToList();
            }
            else//new
            {
                if (Request.QueryString["vendorId"] != null)
                {
                    int vendorId;
                    if (int.TryParse(Request.QueryString["vendorId"], out vendorId))
                    {
                        model.VendorId = vendorId;
                    }
                }
            }
            //vendors
            model.AvailableVendors = _vendorService.GetAllVendors().Select(x => new SelectListItem() { Value = x.Id.ToString(), Text = x.Name }).ToList();
            //product types
            // ProductType.SimpleProduct.ToSelectList(false)
            model.AvailableProductTypes = ProductType.Finials.ToSelectList(false).Where(x => int.Parse(x.Value) > 10)
                .Select(x =>
                    new POTemplate_ProductTypeModel()
                    {
                        ProductTypeId = int.Parse(x.Value),
                        ProductTypeName = ((ProductType)int.Parse(x.Value)).GetLocalizedEnum(_localizationService, _workContext)
                    }
                    ).ToList();

        }

        [HttpPost]
        public ActionResult DeletePOTemplate(int id)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageVendors))
                return AccessDeniedView();

            var poTemplate = _pOTemplateMessageService.GetById(id);
            if (poTemplate == null)
                //No vendor found with the specified id
                return RedirectToAction("List");

            //delete a vendor
            _pOTemplateMessageService.DeletePOTemplateMessage(poTemplate);

            SuccessNotification(_localizationService.GetResource("Admin.Vendors.DeletedPOTemplate"));
            return RedirectToAction("Edit", new { id = poTemplate.VendorId });
        }

        private void SavePOTemplate(POTemplateMessageModel model)
        {

            if (model.Id > 0)
            {
                var poTemplate = _pOTemplateMessageService.GetById(model.Id);
                if (poTemplate == null)
                {
                    return;
                }
                poTemplate.Name = model.Name;
                poTemplate.Active = model.Active;
                poTemplate.BCC = model.BCC;
                poTemplate.CC = model.CC;
                poTemplate.Subject = model.Subject;
                poTemplate.POTemplateHtml = model.POTemplateHtml;
                poTemplate.POTemplateExcel = model.POTemplateExcel;
                poTemplate.VendorId = model.VendorId;
                _pOTemplateMessageService.UpdatePOTemplateMessage(poTemplate);
                //clear old product types
                var _productTypePOTemplateList = _pOTemplateMessageService.GetAllProductTypesOfPOTemplate(model.Id).ToList();
                foreach (var item in _productTypePOTemplateList)
                {
                    _pOTemplateMessageService.DeletePOTemplate_ProductType(item);
                }
            }
            else
            {
                var poTemplate = new POTemplateMessage()
                {
                    Id = model.Id,
                    Name = model.Name,
                    Active = model.Active,
                    BCC = model.BCC,
                    CC = model.CC,
                    Subject = model.Subject,
                    POTemplateHtml = model.POTemplateHtml,
                    POTemplateExcel = model.POTemplateExcel,
                    VendorId = model.VendorId,
                    CreatedOnUtc = model.CreatedOn.HasValue ? model.CreatedOn.Value : DateTime.Now
                };
                _pOTemplateMessageService.InsertPOTemplateMessage(poTemplate);
                model.Id = poTemplate.Id;
            }
            if (model.PostedProductTypes.ProductTypeIds!=null)
            {
                foreach (var pt in model.PostedProductTypes.ProductTypeIds)
                {
                    int _productTypeId;
                    if (int.TryParse(pt, out _productTypeId))
                    {
                        var poTemplate_ProductType = new POTemplate_ProductType()
                        {
                            POTemplateMessageId = model.Id,
                            ProductTypeId = _productTypeId
                        };
                        _pOTemplateMessageService.InsertPOTemplate_ProductType(poTemplate_ProductType);
                    }
                }
            }
           
        }
        #endregion

        #region Vendor notes

        [HttpPost]
        public ActionResult VendorNotesSelect(int vendorId, DataSourceRequest command)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageVendors))
                return AccessDeniedView();

            var vendor = _vendorService.GetVendorById(vendorId);
            if (vendor == null)
                throw new ArgumentException("No vendor found with the specified id");

            var vendorNoteModels = new List<VendorModel.VendorNote>();
            foreach (var vendorNote in vendor.VendorNotes
                .OrderByDescending(vn => vn.CreatedOnUtc))
            {
                vendorNoteModels.Add(new VendorModel.VendorNote
                {
                    Id = vendorNote.Id,
                    VendorId = vendorNote.VendorId,
                    Note = vendorNote.FormatVendorNoteText(),
                    CreatedOn = _dateTimeHelper.ConvertToUserTime(vendorNote.CreatedOnUtc, DateTimeKind.Utc)
                });
            }

            var gridModel = new DataSourceResult
            {
                Data = vendorNoteModels,
                Total = vendorNoteModels.Count
            };

            return Json(gridModel);
        }

        [ValidateInput(false)]
        public ActionResult VendorNoteAdd(int vendorId, string message)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageVendors))
                return AccessDeniedView();

            var vendor = _vendorService.GetVendorById(vendorId);
            if (vendor == null)
                return Json(new { Result = false }, JsonRequestBehavior.AllowGet);

            var vendorNote = new VendorNote
            {
                Note = message,
                CreatedOnUtc = DateTime.UtcNow,
            };
            vendor.VendorNotes.Add(vendorNote);
            _vendorService.UpdateVendor(vendor);

            return Json(new { Result = true }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult VendorNoteDelete(int id, int vendorId)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageVendors))
                return AccessDeniedView();

            var vendor = _vendorService.GetVendorById(vendorId);
            if (vendor == null)
                throw new ArgumentException("No vendor found with the specified id");

            var vendorNote = vendor.VendorNotes.FirstOrDefault(vn => vn.Id == id);
            if (vendorNote == null)
                throw new ArgumentException("No vendor note found with the specified id");
            _vendorService.DeleteVendorNote(vendorNote);

            return new NullJsonResult();
        }

        #endregion

    }
}
