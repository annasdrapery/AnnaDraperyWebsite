using System;
using System.Collections.Generic;
using Nop.Core;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Payments;
using Nop.Core.Domain.Shipping;

namespace Nop.Services.Orders
{
    /// <summary>
    /// Order service interface
    /// </summary>
    public partial interface IPurchaseOrderService
    {

        /// <summary>
        /// Gets an po
        /// </summary>
        /// <param name="orderId">The order identifier</param>
        /// <returns>Order</returns>
        PurchaseOrder GetPOById(int poId);
        IPagedList<PurchaseOrder> GetAllPurchaseOrders(
            int? OrderId,
            int pageIndex = 0,
            int pageSize = int.MaxValue);
        void InsertPO(PurchaseOrder po);
    }
}
