
using System;
using System.Collections.Generic;
using System.Linq;
using Nop.Core.Data;
using Nop.Core.Domain.Catalog;
using Nop.Services.Events;

namespace Nop.Services.Catalog
{
    /// <summary>
    /// Brand template service
    /// </summary>
    public partial class BrandTemplateService : IBrandTemplateService
    {
        #region Fields

        private readonly IRepository<BrandTemplate> _BrandTemplateRepository;
        private readonly IEventPublisher _eventPublisher;

        #endregion
        
        #region Ctor

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="BrandTemplateRepository">Brand template repository</param>
        /// <param name="eventPublisher">Event published</param>
        public BrandTemplateService(IRepository<BrandTemplate> BrandTemplateRepository,
            IEventPublisher eventPublisher)
        {
            this._BrandTemplateRepository = BrandTemplateRepository;
            this._eventPublisher = eventPublisher;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Delete Brand template
        /// </summary>
        /// <param name="BrandTemplate">Brand template</param>
        public virtual void DeleteBrandTemplate(BrandTemplate BrandTemplate)
        {
            if (BrandTemplate == null)
                throw new ArgumentNullException("BrandTemplate");

            _BrandTemplateRepository.Delete(BrandTemplate);

            //event notification
            _eventPublisher.EntityDeleted(BrandTemplate);
        }

        /// <summary>
        /// Gets all Brand templates
        /// </summary>
        /// <returns>Brand templates</returns>
        public virtual IList<BrandTemplate> GetAllBrandTemplates()
        {
            var query = from pt in _BrandTemplateRepository.Table
                        orderby pt.DisplayOrder
                        select pt;

            var templates = query.ToList();
            return templates;
        }

        /// <summary>
        /// Gets a Brand template
        /// </summary>
        /// <param name="BrandTemplateId">Brand template identifier</param>
        /// <returns>Brand template</returns>
        public virtual BrandTemplate GetBrandTemplateById(int BrandTemplateId)
        {
            if (BrandTemplateId == 0)
                return null;

            return _BrandTemplateRepository.GetById(BrandTemplateId);
        }

        /// <summary>
        /// Inserts Brand template
        /// </summary>
        /// <param name="BrandTemplate">Brand template</param>
        public virtual void InsertBrandTemplate(BrandTemplate BrandTemplate)
        {
            if (BrandTemplate == null)
                throw new ArgumentNullException("BrandTemplate");

            _BrandTemplateRepository.Insert(BrandTemplate);

            //event notification
            _eventPublisher.EntityInserted(BrandTemplate);
        }

        /// <summary>
        /// Updates the Brand template
        /// </summary>
        /// <param name="BrandTemplate">Brand template</param>
        public virtual void UpdateBrandTemplate(BrandTemplate BrandTemplate)
        {
            if (BrandTemplate == null)
                throw new ArgumentNullException("BrandTemplate");

            _BrandTemplateRepository.Update(BrandTemplate);

            //event notification
            _eventPublisher.EntityUpdated(BrandTemplate);
        }
        
        #endregion
    }
}
