using System.Collections.Generic;
using Nop.Core;
using Nop.Core.Domain.Vendors;

namespace Nop.Services.Vendors
{
    /// <summary>
    /// Vendor service interface
    /// </summary>
    public partial interface IPOTemplateMessageService
    {

        POTemplateMessage GetById(int id);
        void DeletePOTemplateMessage(POTemplateMessage poTemplate);

        IPagedList<POTemplateMessage> GetAllPOTemplateMessages(int vendorId = 0, 
            int pageIndex = 0, int pageSize = int.MaxValue, bool showHidden = false);

        void InsertPOTemplateMessage(POTemplateMessage poTemplate);

        void UpdatePOTemplateMessage(POTemplateMessage poTemplate);

        void InsertPOTemplate_ProductType(POTemplate_ProductType poTemplate_ProductType);

        void DeletePOTemplate_ProductType(POTemplate_ProductType poTemplate_ProductType);

        IList<POTemplate_ProductType> GetAllProductTypesOfPOTemplate(int poTemplateId);

    }
}