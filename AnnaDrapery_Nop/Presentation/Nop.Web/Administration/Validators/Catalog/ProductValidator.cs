using FluentValidation;
using Nop.Admin.Models.Catalog;
using Nop.Core.Domain.Catalog;
using Nop.Services.Localization;
using Nop.Web.Framework.Validators;

namespace Nop.Admin.Validators.Catalog
{
    public class ProductValidator : BaseNopValidator<ProductModel>
    {
        public ProductValidator(ILocalizationService localizationService)
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage(localizationService.GetResource("Admin.Catalog.Products.Fields.Name.Required"));
            RuleFor(x => x.FinialProductId).NotEmpty()
                .WithMessage("Please select a Finial")
                .When(x=>x.ProductTypeId == (int)ProductType.CompleteRodSets);
            RuleFor(x => x.PoleProductId).NotEmpty()
                .WithMessage("Please select a Pole")
                .When(x => x.ProductTypeId == (int)ProductType.CompleteRodSets);
        }
    }
}