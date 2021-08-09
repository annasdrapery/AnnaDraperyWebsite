using System;
using System.Collections.Generic;
using System.Linq;
using Nop.Core;
using Nop.Core.Caching;
using Nop.Core.Data;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Security;
using Nop.Core.Domain.Stores;
using Nop.Services.Customers;
using Nop.Services.Events;

namespace Nop.Services.Orders
{
    public partial class PurchaseOrderService : IPurchaseOrderService
    {

        #region Constants
        private const string POS_BY_ID_KEY = "Nop.po.id-{0}";

        /// <summary>
        /// Key pattern to clear cache
        /// </summary>
        private const string POS_PATTERN_KEY = "Nop.po.";

        #endregion

        #region Fields

        private readonly IRepository<PurchaseOrder> _poRepository;
        private readonly IWorkContext _workContext;
        private readonly IEventPublisher _eventPublisher;
        private readonly ICacheManager _cacheManager;
        private readonly CatalogSettings _catalogSettings;

        #endregion

        #region Ctor

        public PurchaseOrderService(ICacheManager cacheManager,
            IRepository<PurchaseOrder> poRepository,
            IRepository<Product> productRepository,
            IWorkContext workContext,
            CatalogSettings catalogSettings,
            IEventPublisher eventPublisher)
        {
            this._cacheManager = cacheManager;
            this._poRepository = poRepository;
            this._workContext = workContext;
            this._catalogSettings = catalogSettings;
            this._eventPublisher = eventPublisher;
        }
        #endregion

        #region Methods


        public virtual IPagedList<PurchaseOrder> GetAllPurchaseOrders(int? OrderId, int pageIndex = 0, int pageSize = int.MaxValue)
        {
            var query = _poRepository.Table;

            query = query.Where(m => !m.Deleted);
            if (OrderId.HasValue)
            {
                query = query.Where(m => m.OrderId == OrderId.Value);
            }
            query = query.OrderBy(m => m.CreatedOnUtc);
            return new PagedList<PurchaseOrder>(query, pageIndex, pageSize);
        }

        public virtual PurchaseOrder GetPOById(int poId)
        {
            if (poId == 0)
                return null;

            string key = string.Format(POS_BY_ID_KEY, poId);
            return _cacheManager.Get(key, () => _poRepository.GetById(poId));
        }

        public void InsertPO(PurchaseOrder po)
        {
            if (po == null)
                throw new ArgumentNullException("purchaseorder");

            _poRepository.Insert(po);

            //event notification
            _eventPublisher.EntityInserted(po);
        }
        #endregion
    }
}
