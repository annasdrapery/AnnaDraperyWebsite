using Nop.Core.Domain.Vendors;

namespace Nop.Data.Mapping.Vendors
{
    public partial class POTemplateMessageMap : NopEntityTypeConfiguration<POTemplateMessage>
    {
        public POTemplateMessageMap()
        {
            this.ToTable("POTemplateMessage");
            this.HasKey(x => x.Id);

            this.Property(x => x.Name).IsRequired().HasMaxLength(400);
            this.Property(x => x.Subject).HasMaxLength(500);
            this.Property(x => x.CC).HasMaxLength(500);
        }
    }
}