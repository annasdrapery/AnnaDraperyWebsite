using System;

namespace Nop.Core.Domain.Vendors
{
    public partial class POTemplate_ProductType : BaseEntity
    {
        public int POTemplateMessageId { get; set; }
        public int ProductTypeId { get; set; }
        public virtual POTemplateMessage POTemplateMessage { get; set; }
    }

}
