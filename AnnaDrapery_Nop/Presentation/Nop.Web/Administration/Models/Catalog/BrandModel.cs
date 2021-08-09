using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;
using FluentValidation.Attributes;
using Nop.Admin.Models.Customers;
using Nop.Admin.Models.Discounts;
using Nop.Admin.Models.Stores;
using Nop.Admin.Validators.Catalog;
using Nop.Web.Framework;
using Nop.Web.Framework.Localization;
using Nop.Web.Framework.Mvc;

namespace Nop.Admin.Models.Catalog
{
    [Validator(typeof(BrandValidator))]
    public partial class BrandModel : BaseNopEntityModel, ILocalizedModel<BrandLocalizedModel>
    {
        public BrandModel()
        {
            if (PageSize < 1)
            {
                PageSize = 5;
            }
            Locales = new List<BrandLocalizedModel>();
            AvailableBrandTemplates = new List<SelectListItem>();
        }
        [NopResourceDisplayName("Admin.Catalog.Brands.Fields.BrandTemplate")]
        public int BrandTemplateId { get; set; }
        public IList<SelectListItem> AvailableBrandTemplates { get; set; }

        [NopResourceDisplayName("Admin.Catalog.Brands.Fields.Name")]
        [AllowHtml]
        public string Name { get; set; }

        [NopResourceDisplayName("Admin.Catalog.Brands.Fields.Description")]
        [AllowHtml]
        public string Description { get; set; }


        [NopResourceDisplayName("Admin.Catalog.Brands.Fields.MetaKeywords")]
        [AllowHtml]
        public string MetaKeywords { get; set; }

        [NopResourceDisplayName("Admin.Catalog.Brands.Fields.MetaDescription")]
        [AllowHtml]
        public string MetaDescription { get; set; }

        [NopResourceDisplayName("Admin.Catalog.Brands.Fields.MetaTitle")]
        [AllowHtml]
        public string MetaTitle { get; set; }

        [NopResourceDisplayName("Admin.Catalog.Brands.Fields.SeName")]
        [AllowHtml]
        public string SeName { get; set; }

        [UIHint("Picture")]
        [NopResourceDisplayName("Admin.Catalog.Brands.Fields.Picture")]
        public int PictureId { get; set; }

        [NopResourceDisplayName("Admin.Catalog.Brands.Fields.PageSize")]
        public int PageSize { get; set; }

        [NopResourceDisplayName("Admin.Catalog.Brands.Fields.AllowCustomersToSelectPageSize")]
        public bool AllowCustomersToSelectPageSize { get; set; }

        [NopResourceDisplayName("Admin.Catalog.Brands.Fields.PageSizeOptions")]
        public string PageSizeOptions { get; set; }

        [NopResourceDisplayName("Admin.Catalog.Brands.Fields.Published")]
        public bool Published { get; set; }

        [NopResourceDisplayName("Admin.Catalog.Brands.Fields.Deleted")]
        public bool Deleted { get; set; }

        [NopResourceDisplayName("Admin.Catalog.Brands.Fields.DisplayOrder")]
        public int DisplayOrder { get; set; }

        public IList<BrandLocalizedModel> Locales { get; set; }

        public string PictureThumbnailUrl { get; set; }

      
    }

    public partial class BrandLocalizedModel : ILocalizedModelLocal
    {
        public int LanguageId { get; set; }

        [NopResourceDisplayName("Admin.Catalog.Brands.Fields.Name")]
        [AllowHtml]
        public string Name { get; set; }

        [NopResourceDisplayName("Admin.Catalog.Brands.Fields.Description")]
        [AllowHtml]
        public string Description {get;set;}

        [NopResourceDisplayName("Admin.Catalog.Brands.Fields.MetaKeywords")]
        [AllowHtml]
        public string MetaKeywords { get; set; }

        [NopResourceDisplayName("Admin.Catalog.Brands.Fields.MetaDescription")]
        [AllowHtml]
        public string MetaDescription { get; set; }

        [NopResourceDisplayName("Admin.Catalog.Brands.Fields.MetaTitle")]
        [AllowHtml]
        public string MetaTitle { get; set; }

        [NopResourceDisplayName("Admin.Catalog.Brands.Fields.SeName")]
        [AllowHtml]
        public string SeName { get; set; }
    }
}