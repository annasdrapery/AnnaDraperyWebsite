using System.Web.Mvc;
using Nop.Web.Framework;
using Nop.Web.Framework.Mvc;

namespace Nop.Admin.Models.Catalog
{
    public partial class BrandListModel : BaseNopModel
    {
        [NopResourceDisplayName("Admin.Catalog.Brands.List.SearchBrandName")]
        [AllowHtml]
        public string SearchBrandName { get; set; }
    }
}