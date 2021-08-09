using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;
using FluentValidation.Attributes;
using Nop.Admin.Validators.Vendors;
using Nop.Web.Framework;
using Nop.Web.Framework.Localization;
using Nop.Web.Framework.Mvc;

namespace Nop.Admin.Models.Vendors
{
    [Validator(typeof(POTemplateMessageValidator))]
    public partial class POTemplateMessageModel : BaseNopEntityModel
    {
        public POTemplateMessageModel()
        {
            AvailableVendors = new List<SelectListItem>();
            AvailableProductTypes = new List<POTemplate_ProductTypeModel>();
            SelectedProductTypes = new List<POTemplate_ProductTypeModel>();
        }
        [NopResourceDisplayName("Admin.POTemplateMessage.Fields.Vendor")]
        public int VendorId { get; set; }
        public IList<SelectListItem> AvailableVendors { get; set; }
        [NopResourceDisplayName("Admin.POTemplateMessage.Fields.Name")]
        [AllowHtml]
        public string Name { get; set; }

        [NopResourceDisplayName("Admin.POTemplateMessage.Fields.Subject")]
        [AllowHtml]
        public string Subject { get; set; }

        [NopResourceDisplayName("Admin.POTemplateMessage.Fields.POTemplateHtml")]
        [AllowHtml]
        public string POTemplateHtml { get; set; }

        [NopResourceDisplayName("Admin.POTemplateMessage.Fields.POTemplateExcel")]
        [AllowHtml]
        public string POTemplateExcel { get; set; }

        [NopResourceDisplayName("Admin.POTemplateMessage.Fields.Active")]
        public bool Active { get; set; }

        [NopResourceDisplayName("Admin.POTemplateMessage.Fields.CC")]
        [AllowHtml]
        public string CC { get; set; }

        [NopResourceDisplayName("Admin.POTemplateMessage.Fields.BCC")]
        [AllowHtml]
        public string BCC { get; set; }

        [NopResourceDisplayName("Admin.POTemplateMessage.Fields.CreatedOn")]
        public DateTime? CreatedOn { get; set; }

        [NopResourceDisplayName("Admin.POTemplateMessage.Fields.ProductTypes")]
        public string ProductTypesDisplay { get; set; }

        public IEnumerable<POTemplate_ProductTypeModel> AvailableProductTypes { get; set; }
        public IEnumerable<POTemplate_ProductTypeModel> SelectedProductTypes { get; set; }

        private PostedProductTypes _postedProductTypes;
        public PostedProductTypes PostedProductTypes { get { return _postedProductTypes ?? new PostedProductTypes(); } set { _postedProductTypes = value; } }       

    }
    public partial class POTemplate_ProductTypeModel : BaseNopEntityModel
    {
        public int POTemplateMessageId { get; set; }
        public int ProductTypeId { get; set; }
        public string ProductTypeName { get; set; }
        public bool IsSelected { get; set; }
    }
    public class PostedProductTypes
    {
        //this array will be used to POST values from the form to the controller
        public string[] ProductTypeIds { get; set; }
    }

}