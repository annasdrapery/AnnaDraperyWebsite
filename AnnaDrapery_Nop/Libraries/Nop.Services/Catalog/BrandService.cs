using System;
using System.Collections.Generic;
using System.Linq;
using Nop.Core;
using Nop.Core.Caching;
using Nop.Core.Data;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Security;
using Nop.Core.Domain.Stores;
using Nop.Services.Customers;
using Nop.Services.Events;

namespace Nop.Services.Catalog
{
    /// <summary>
    /// Manufacturer service
    /// </summary>
    public partial class BrandService : IBrandService
    {
        #region Constants
        /// <summary>
        /// Key for caching
        /// </summary>
        /// <remarks>
        /// {0} : manufacturer ID
        /// </remarks>
        private const string BRANDS_BY_ID_KEY = "Nop.brand.id-{0}";
 
        /// <summary>
        /// Key pattern to clear cache
        /// </summary>
        private const string BRANDS_PATTERN_KEY = "Nop.brand.";

        #endregion

        #region Fields

        private readonly IRepository<Brand> _brandRepository;
        private readonly IRepository<Product> _productRepository;
        private readonly IWorkContext _workContext;
        private readonly IEventPublisher _eventPublisher;
        private readonly ICacheManager _cacheManager;
        private readonly CatalogSettings _catalogSettings;

        #endregion

        #region Ctor

        public BrandService(ICacheManager cacheManager,
            IRepository<Brand> brandRepository,
            IRepository<Product> productRepository,
            IWorkContext workContext,
            CatalogSettings catalogSettings,
            IEventPublisher eventPublisher)
        {
            this._cacheManager = cacheManager;
            this._brandRepository = brandRepository;
            this._productRepository = productRepository;
            this._workContext = workContext;
            this._catalogSettings = catalogSettings;
            this._eventPublisher = eventPublisher;
        }
        #endregion

        #region Methods

        public virtual void DeleteBrand(Brand brand)
        {
            if (brand == null)
                throw new ArgumentNullException("brand");
            
            brand.Deleted = true;
            UpdateBrand(brand);
        }

        public virtual IPagedList<Brand> GetAllBrands(string brandName = "",
            int pageIndex = 0,
            int pageSize = int.MaxValue, 
            bool showHidden = false)
        {
            var query = _brandRepository.Table;
            if (!showHidden)
                query = query.Where(m => m.Published);
            if (!String.IsNullOrWhiteSpace(brandName))
                query = query.Where(m => m.Name.Contains(brandName));
            query = query.Where(m => !m.Deleted);
            query = query.OrderBy(m => m.DisplayOrder);

            if (!showHidden)
            { 
               
                //only distinct brand (group by ID)
                query = from m in query
                        group m by m.Id
                            into mGroup
                            orderby mGroup.Key
                            select mGroup.FirstOrDefault();
                query = query.OrderBy(m => m.DisplayOrder);
            }

            return new PagedList<Brand>(query, pageIndex, pageSize);
        }

        public virtual Brand GetBrandById(int brandId)
        {
            if (brandId == 0)
                return null;
            
            string key = string.Format(BRANDS_BY_ID_KEY, brandId);
            return _cacheManager.Get(key, () => _brandRepository.GetById(brandId));
        }

        public virtual void InsertBrand(Brand brand)
        {
            if (brand == null)
                throw new ArgumentNullException("brand");

            _brandRepository.Insert(brand);

            //cache
            _cacheManager.RemoveByPattern(BRANDS_PATTERN_KEY);

            //event notification
            _eventPublisher.EntityInserted(brand);
        }

        public virtual void UpdateBrand(Brand brand)
        {
            if (brand == null)
                throw new ArgumentNullException("brand");

            _brandRepository.Update(brand);

            //cache
            _cacheManager.RemoveByPattern(BRANDS_PATTERN_KEY);

            //event notification
            _eventPublisher.EntityUpdated(brand);
        }
        

        #endregion

    }
}
