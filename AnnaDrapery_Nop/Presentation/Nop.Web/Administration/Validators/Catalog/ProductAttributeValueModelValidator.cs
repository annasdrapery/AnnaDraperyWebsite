using FluentValidation;
using Nop.Admin.Models.Catalog;
using Nop.Core.Domain.Catalog;
using Nop.Services.Localization;
using Nop.Web.Framework.Validators;

namespace Nop.Admin.Validators.Catalog
{
    public class ProductAttributeValueModelValidator : BaseNopValidator<ProductModel.ProductAttributeValueModel>
    {
        public ProductAttributeValueModelValidator(ILocalizationService localizationService)
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage(localizationService.GetResource("Admin.Catalog.Products.ProductAttributes.Attributes.Values.Fields.Name.Required"));

            //RuleFor(x => x.FinialPartNumber)
            //   .NotEmpty()
            //   .WithMessage("Please provide Finial Part Number for Product is Complete Rod Sets")
            //   .When(x => x.ProductTypeId == (int)ProductType.CompleteRodSets);
            //RuleFor(x => x.PolePartNumber)
            //   .NotEmpty()
            //   .WithMessage("Please provide Pole Part Number for Product is Complete Rod Sets")
            //   .When(x => x.ProductTypeId == (int)ProductType.CompleteRodSets);
            //RuleFor(x => x.BracketPartNumber)
            //   .NotEmpty()
            //   .WithMessage("Please provide Bracket Part Number for Product is Complete Rod Sets")
            //   .When(x => x.ProductTypeId == (int)ProductType.CompleteRodSets);
            //RuleFor(x => x.RingPartNumber)
            //   .NotEmpty()
            //   .WithMessage("Please provide Ring Part Number for Product is Complete Rod Sets")
            //   .When(x => x.ProductTypeId == (int)ProductType.CompleteRodSets);

            RuleFor(x => x.Quantity)
                .GreaterThanOrEqualTo(1)
                .WithMessage(localizationService.GetResource("Admin.Catalog.Products.ProductAttributes.Attributes.Values.Fields.Quantity.GreaterThanOrEqualTo1"))
                .When(x => x.AttributeValueTypeId == (int)AttributeValueType.AssociatedToProduct);
        }
    }
}