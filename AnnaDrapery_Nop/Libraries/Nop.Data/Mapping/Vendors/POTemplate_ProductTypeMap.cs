
using Nop.Core.Domain.Vendors;

namespace Nop.Data.Mapping.Vendors
{
    public partial class POTemplate_ProductTypeMap : NopEntityTypeConfiguration<POTemplate_ProductType>
    {
        public POTemplate_ProductTypeMap()
        {
            this.ToTable("POTemplate_ProductType");
            this.HasKey(x => x.Id);
            this.Property(x => x.ProductTypeId).IsRequired();

            this.HasRequired(x => x.POTemplateMessage)
                .WithMany(x => x.POTemplate_ProductTypes)
                .HasForeignKey(x => x.POTemplateMessageId);
        }
    }
}