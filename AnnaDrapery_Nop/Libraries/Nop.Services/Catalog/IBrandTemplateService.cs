using System.Collections.Generic;
using Nop.Core.Domain.Catalog;

namespace Nop.Services.Catalog
{
    /// <summary>
    /// Brand template service interface
    /// </summary>
    public partial interface IBrandTemplateService
    {
        /// <summary>
        /// Delete template
        /// </summary>
        /// <param name="Template">Brand template</param>
        void DeleteBrandTemplate(BrandTemplate BrandTemplate);

        /// <summary>
        /// Gets all Brand templates
        /// </summary>
        /// <returns>Brand templates</returns>
        IList<BrandTemplate> GetAllBrandTemplates();

        /// <summary>
        /// Gets a Brand template
        /// </summary>
        /// <param name="BrandTemplateId">Brand template identifier</param>
        /// <returns>Brand template</returns>
        BrandTemplate GetBrandTemplateById(int BrandTemplateId);

        /// <summary>
        /// Inserts Brand template
        /// </summary>
        /// <param name="BrandTemplate">Brand template</param>
        void InsertBrandTemplate(BrandTemplate BrandTemplate);

        /// <summary>
        /// Updates the Brand template
        /// </summary>
        /// <param name="BrandTemplate">Brand template</param>
        void UpdateBrandTemplate(BrandTemplate BrandTemplate);
    }
}
