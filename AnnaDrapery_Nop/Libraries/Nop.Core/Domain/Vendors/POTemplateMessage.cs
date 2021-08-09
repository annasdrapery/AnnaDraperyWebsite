using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using Nop.Core.Domain.Common;
using Nop.Core.Domain.Customers;

namespace Nop.Core.Domain.Vendors
{
    public partial class POTemplateMessage : BaseEntity
    {
        public string Name { get; set; }
        public int VendorId { get; set; }
        public string POTemplateHtml { get; set; }
        public string POTemplateExcel { get; set; }
        public bool Deleted { get; set; }
        public DateTime CreatedOnUtc { get; set; }
        public bool Active { get; set; }
        public string CC { get; set; }
        public string BCC { get; set; }
        public string Subject { get; set; }

        private ICollection<POTemplate_ProductType> _POTemplate_ProductTypes;
        public virtual ICollection<POTemplate_ProductType> POTemplate_ProductTypes
        {
            get { return _POTemplate_ProductTypes ?? (_POTemplate_ProductTypes = new List<POTemplate_ProductType>()); }
            protected set { _POTemplate_ProductTypes = value; }
        }
    }
}
