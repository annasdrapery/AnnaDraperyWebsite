using System.Collections.Generic;
using Nop.Core;
using Nop.Core.Domain.Catalog;

namespace Nop.Services.Catalog
{
    /// <summary>
    /// Manufacturer service
    /// </summary>
    public partial interface IBrandService
    {
        /// <summary>
        /// Deletes a brand
        /// </summary>
        /// <param name="manufacturer">Brand</param>
        void DeleteBrand(Brand brand);
        
        /// <summary>
        /// Gets all brands
        /// </summary>
        /// <param name="brandName">Brand name</param>
        /// <param name="pageIndex">Page index</param>
        /// <param name="pageSize">Page size</param>
        /// <param name="showHidden">A value indicating whether to show hidden records</param>
        /// <returns>Brand</returns>
        IPagedList<Brand> GetAllBrands(string brandName = "",
            int pageIndex = 0,
            int pageSize = int.MaxValue,
            bool showHidden = false);

        /// <summary>
        /// Gets a brand
        /// </summary>
        /// <param name="brandId">Brand identifier</param>
        /// <returns>Brand</returns>
        Brand GetBrandById(int brandId);

        /// <summary>
        /// Inserts a brand
        /// </summary>
        /// <param name="brand">brand</param>
        void InsertBrand(Brand brand);

        /// <summary>
        /// Updates the brand
        /// </summary>
        /// <param name="brand">Brand</param>
        void UpdateBrand(Brand brand);
        

    }
}
