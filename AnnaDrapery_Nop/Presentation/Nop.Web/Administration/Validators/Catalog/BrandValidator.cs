using FluentValidation;
using Nop.Admin.Models.Catalog;
using Nop.Services.Localization;
using Nop.Web.Framework.Validators;

namespace Nop.Admin.Validators.Catalog
{
    public class BrandValidator : BaseNopValidator<BrandModel>
    {
        public BrandValidator(ILocalizationService localizationService)
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage(localizationService.GetResource("Admin.Catalog.Brands.Fields.Name.Required"));
        }
    }
}