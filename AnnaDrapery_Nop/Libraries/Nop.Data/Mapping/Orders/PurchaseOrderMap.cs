using Nop.Core.Domain.Orders;

namespace Nop.Data.Mapping.Orders
{
    public partial class PurchaseOrderMap : NopEntityTypeConfiguration<PurchaseOrder>
    {
        public PurchaseOrderMap()
        {
            this.ToTable("PurchaseOrder");
            this.HasKey(o => o.Id);
            this.Property(o => o.OrderId).IsRequired();
            this.Property(o => o.VendorId).IsRequired();

        }
    }
}