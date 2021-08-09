using System.Web.Mvc;
using FluentValidation.Attributes;
using Nop.Admin.Validators.Templates;
using Nop.Web.Framework;
using Nop.Web.Framework.Mvc;

namespace Nop.Admin.Models.Templates
{
    [Validator(typeof(BrandTemplateValidator))]
    public partial class BrandTemplateModel : BaseNopEntityModel
    {
        [NopResourceDisplayName("Admin.System.Templates.Manufacturer.Name")]
        [AllowHtml]
        public string Name { get; set; }

        [NopResourceDisplayName("Admin.System.Templates.Manufacturer.ViewPath")]
        [AllowHtml]
        public string ViewPath { get; set; }

        [NopResourceDisplayName("Admin.System.Templates.Manufacturer.DisplayOrder")]
        public int DisplayOrder { get; set; }
    }
}