using FluentValidation;
using Nop.Admin.Models.Vendors;
using Nop.Services.Localization;
using Nop.Web.Framework.Validators;

namespace Nop.Admin.Validators.Vendors
{
    public class POTemplateMessageValidator : BaseNopValidator<POTemplateMessageModel>
    {
        public POTemplateMessageValidator(ILocalizationService localizationService)
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage(localizationService.GetResource("Admin.POTemplateMessage.Fields.Name.Required"));
        }
    }
}