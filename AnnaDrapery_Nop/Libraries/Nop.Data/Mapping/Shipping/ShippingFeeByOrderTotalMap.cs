using Nop.Core.Domain.Shipping;

namespace Nop.Data.Mapping.Shipping
{
    public partial class ShippingFeeByOrderTotalMap : NopEntityTypeConfiguration<ShippingFeeByOrderTotal>
    {
        public ShippingFeeByOrderTotalMap()
        {
            this.ToTable("ShippingFeeByOrderTotal");
            this.HasKey(s => s.Id);

            
        }
    }
}