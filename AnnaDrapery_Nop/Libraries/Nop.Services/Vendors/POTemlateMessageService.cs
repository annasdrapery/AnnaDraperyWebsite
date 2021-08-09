using System;
using System.Collections.Generic;
using System.Linq;
using Nop.Core;
using Nop.Core.Data;
using Nop.Core.Domain.Vendors;
using Nop.Services.Events;

namespace Nop.Services.Vendors
{

    public partial class POTemlateMessageService : IPOTemplateMessageService
    {
        #region Fields

        private readonly IRepository<POTemplateMessage> _poTemplateRepository;
        private readonly IRepository<POTemplate_ProductType> _poTemplateProductTypeNoteRepository;
        private readonly IEventPublisher _eventPublisher;

        #endregion

        #region Ctor
        public POTemlateMessageService(IRepository<POTemplateMessage> poTemplateRepository,
            IRepository<POTemplate_ProductType> poTemplateProductTypeNoteRepository,
            IEventPublisher eventPublisher)
        {
            this._poTemplateRepository = poTemplateRepository;
            this._poTemplateProductTypeNoteRepository = poTemplateProductTypeNoteRepository;
            this._eventPublisher = eventPublisher;
        }

        #endregion

        #region Methods

        public POTemplateMessage GetById(int id)
        {
            if (id == 0)
                return null;

            return _poTemplateRepository.GetById(id);
        }

        public void DeletePOTemplateMessage(POTemplateMessage poTemplate)
        {
            if (poTemplate == null)
                throw new ArgumentNullException("poTemplate");

            poTemplate.Deleted = true;
            UpdatePOTemplateMessage(poTemplate);
        }

        public IPagedList<POTemplateMessage> GetAllPOTemplateMessages(int vendorId = 0, int pageIndex = 0, int pageSize = int.MaxValue, bool showHidden = false)
        {
            var query = _poTemplateRepository.Table;
            if (vendorId >0)
                query = query.Where(x=>x.VendorId == vendorId);
            if (!showHidden)
                query = query.Where(v => v.Active);
            query = query.Where(v => !v.Deleted);
            query = query.OrderBy(v => v.Name);

            var templates = new PagedList<POTemplateMessage>(query, pageIndex, pageSize);
            return templates;
        }

        public void InsertPOTemplateMessage(POTemplateMessage poTemplate)
        {
            if (poTemplate == null)
                throw new ArgumentNullException("poTemplate");

            _poTemplateRepository.Insert(poTemplate);

            //event notification
            _eventPublisher.EntityInserted(poTemplate);
        }

        public void UpdatePOTemplateMessage(POTemplateMessage poTemplate)
        {
            if (poTemplate == null)
                throw new ArgumentNullException("poTemplate");

            _poTemplateRepository.Update(poTemplate);

            //event notification
            _eventPublisher.EntityUpdated(poTemplate);
        }

        public void InsertPOTemplate_ProductType(POTemplate_ProductType poTemplate_ProductType)
        {
            if (poTemplate_ProductType == null)
                throw new ArgumentNullException("poTemplate_ProductType");
            _poTemplateProductTypeNoteRepository.Insert(poTemplate_ProductType);
            //event notification
            _eventPublisher.EntityInserted(poTemplate_ProductType);
        }

        public void DeletePOTemplate_ProductType(POTemplate_ProductType poTemplate_ProductType)
        {
            if (poTemplate_ProductType == null)
                throw new ArgumentNullException("poTemplate_ProductType");
            _poTemplateProductTypeNoteRepository.Delete(poTemplate_ProductType);
            //event notification
            _eventPublisher.EntityDeleted(poTemplate_ProductType);
        }
        #endregion  
    

        public IList<POTemplate_ProductType> GetAllProductTypesOfPOTemplate(int poTemplateId)
        {
            var query = _poTemplateProductTypeNoteRepository.Table;

            query = query.Where(x=>x.POTemplateMessageId==poTemplateId);

            var pt = new List<POTemplate_ProductType>(query);
            return pt;
        }
    }
}