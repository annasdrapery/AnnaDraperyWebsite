using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using Nop.Core;
using Nop.Core.Domain.Blogs;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Forums;
using Nop.Core.Domain.Messages;
using Nop.Core.Domain.News;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Shipping;
using Nop.Core.Domain.Stores;
using Nop.Core.Domain.Vendors;
using Nop.Services.Catalog;
using Nop.Services.Customers;
using Nop.Services.Events;
using Nop.Services.Localization;
using Nop.Services.Stores;

namespace Nop.Services.Messages
{
    public partial class WorkflowMessageService : IWorkflowMessageService
    {
        #region Fields

        private readonly IMessageTemplateService _messageTemplateService;
        private readonly IQueuedEmailService _queuedEmailService;
        private readonly ILanguageService _languageService;
        private readonly ITokenizer _tokenizer;
        private readonly IEmailAccountService _emailAccountService;
        private readonly IMessageTokenProvider _messageTokenProvider;
        private readonly IStoreService _storeService;
        private readonly IStoreContext _storeContext;
        private readonly EmailAccountSettings _emailAccountSettings;
        private readonly IEventPublisher _eventPublisher;
        private readonly IProductAttributeParser _productAttributeParser;
        private readonly IProductAttributeService _productAttributeService;
        #endregion

        #region Ctor

        public WorkflowMessageService(IMessageTemplateService messageTemplateService,
            IQueuedEmailService queuedEmailService,
            ILanguageService languageService,
            ITokenizer tokenizer, 
            IEmailAccountService emailAccountService,
            IMessageTokenProvider messageTokenProvider,
            IStoreService storeService,
            IStoreContext storeContext,
            EmailAccountSettings emailAccountSettings,
            IProductAttributeParser productAttributeParser,
            IProductAttributeService productAttributeService,
            IEventPublisher eventPublisher)
        {
            this._messageTemplateService = messageTemplateService;
            this._queuedEmailService = queuedEmailService;
            this._languageService = languageService;
            this._tokenizer = tokenizer;
            this._emailAccountService = emailAccountService;
            this._messageTokenProvider = messageTokenProvider;
            this._storeService = storeService;
            this._storeContext = storeContext;
            this._emailAccountSettings = emailAccountSettings;
            this._eventPublisher = eventPublisher;
            this._productAttributeParser = productAttributeParser;
            this._productAttributeService = productAttributeService;
        }

        #endregion

        #region Utilities
        
        protected virtual int SendNotification(MessageTemplate messageTemplate, 
            EmailAccount emailAccount, int languageId, IEnumerable<Token> tokens,
            string toEmailAddress, string toName,
            string attachmentFilePath = null, string attachmentFileName = null,
            string replyToEmailAddress = null, string replyToName = null)
        {
            //retrieve localized message template data
            var bcc = messageTemplate.GetLocalized(mt => mt.BccEmailAddresses, languageId);
            var subject = messageTemplate.GetLocalized(mt => mt.Subject, languageId);
            var body = messageTemplate.GetLocalized(mt => mt.Body, languageId);

            //Replace subject and body tokens 
            var subjectReplaced = _tokenizer.Replace(subject, tokens, false);
            var bodyReplaced = _tokenizer.Replace(body, tokens, true);
            
            var email = new QueuedEmail
            {
                Priority = QueuedEmailPriority.High,
                From = emailAccount.Email,
                FromName = emailAccount.DisplayName,
                To = toEmailAddress,
                ToName = toName,
                ReplyTo = replyToEmailAddress,
                ReplyToName = replyToName,
                CC = string.Empty,
                Bcc = bcc,
                Subject = subjectReplaced,
                Body = bodyReplaced,
                AttachmentFilePath = attachmentFilePath,
                AttachmentFileName = attachmentFileName,
                AttachedDownloadId = messageTemplate.AttachedDownloadId,
                CreatedOnUtc = DateTime.UtcNow,
                EmailAccountId = emailAccount.Id
            };
            messageTemplate.Body = email.Body;
            _queuedEmailService.InsertQueuedEmail(email);
            return email.Id;
        }

        protected virtual MessageTemplate GetActiveMessageTemplate(string messageTemplateName, int storeId)
        {
            var messageTemplate = _messageTemplateService.GetMessageTemplateByName(messageTemplateName, storeId);

            //no template found
            if (messageTemplate == null)
                return null;

            //ensure it's active
            var isActive = messageTemplate.IsActive;
            if (!isActive)
                return null;

            return messageTemplate;
        }

        protected virtual EmailAccount GetEmailAccountOfMessageTemplate(MessageTemplate messageTemplate, int languageId)
        {
            var emailAccounId = messageTemplate.GetLocalized(mt => mt.EmailAccountId, languageId);
            var emailAccount = _emailAccountService.GetEmailAccountById(emailAccounId);
            if (emailAccount == null)
                emailAccount = _emailAccountService.GetEmailAccountById(_emailAccountSettings.DefaultEmailAccountId);
            if (emailAccount == null)
                emailAccount = _emailAccountService.GetAllEmailAccounts().FirstOrDefault();
            return emailAccount;

        }

        protected virtual int EnsureLanguageIsActive(int languageId, int storeId)
        {
            //load language by specified ID
            var language = _languageService.GetLanguageById(languageId);

            if (language == null || !language.Published)
            {
                //load any language from the specified store
                language = _languageService.GetAllLanguages(storeId: storeId).FirstOrDefault();
            }
            if (language == null || !language.Published)
            {
                //load any language
                language = _languageService.GetAllLanguages().FirstOrDefault();
            }

            if (language == null)
                throw new Exception("No active language could be loaded");
            return language.Id;
        }

        #endregion

        #region Methods

        #region Customer workflow

        /// <summary>
        /// Sends 'New customer' notification message to a store owner
        /// </summary>
        /// <param name="customer">Customer instance</param>
        /// <param name="languageId">Message language identifier</param>
        /// <returns>Queued email identifier</returns>
        public virtual int SendCustomerRegisteredNotificationMessage(Customer customer, int languageId)
        {
            if (customer == null)
                throw new ArgumentNullException("customer");

            var store = _storeContext.CurrentStore;
            languageId = EnsureLanguageIsActive(languageId, store.Id);

            var messageTemplate = GetActiveMessageTemplate("NewCustomer.Notification", store.Id);
            if (messageTemplate == null)
                return 0;

            //email account
            var emailAccount = GetEmailAccountOfMessageTemplate(messageTemplate, languageId);

            //tokens
            var tokens = new List<Token>();
            _messageTokenProvider.AddStoreTokens(tokens, store, emailAccount);
            _messageTokenProvider.AddCustomerTokens(tokens, customer);

            //event notification
            _eventPublisher.MessageTokensAdded(messageTemplate, tokens);

            var toEmail = emailAccount.Email;
            var toName = emailAccount.DisplayName;
            return SendNotification(messageTemplate, emailAccount,
                languageId, tokens,
                toEmail, toName);
        }

        /// <summary>
        /// Sends a welcome message to a customer
        /// </summary>
        /// <param name="customer">Customer instance</param>
        /// <param name="languageId">Message language identifier</param>
        /// <returns>Queued email identifier</returns>
        public virtual int SendCustomerWelcomeMessage(Customer customer, int languageId)
        {
            if (customer == null)
                throw new ArgumentNullException("customer");

            var store = _storeContext.CurrentStore;
            languageId = EnsureLanguageIsActive(languageId, store.Id);

            var messageTemplate = GetActiveMessageTemplate("Customer.WelcomeMessage", store.Id);
            if (messageTemplate == null)
                return 0;

            //email account
            var emailAccount = GetEmailAccountOfMessageTemplate(messageTemplate, languageId);

            //tokens
            var tokens = new List<Token>();
            _messageTokenProvider.AddStoreTokens(tokens, store, emailAccount);
            _messageTokenProvider.AddCustomerTokens(tokens, customer);

            //event notification
            _eventPublisher.MessageTokensAdded(messageTemplate, tokens);

            var toEmail = customer.Email;
            var toName = customer.GetFullName();
            return SendNotification(messageTemplate, emailAccount,
                languageId, tokens, 
                toEmail, toName);
        }

        /// <summary>
        /// Sends an email validation message to a customer
        /// </summary>
        /// <param name="customer">Customer instance</param>
        /// <param name="languageId">Message language identifier</param>
        /// <returns>Queued email identifier</returns>
        public virtual int SendCustomerEmailValidationMessage(Customer customer, int languageId)
        {
            if (customer == null)
                throw new ArgumentNullException("customer");

            var store = _storeContext.CurrentStore;
            languageId = EnsureLanguageIsActive(languageId, store.Id);

            var messageTemplate = GetActiveMessageTemplate("Customer.EmailValidationMessage", store.Id);
            if (messageTemplate == null)
                return 0;

            //email account
            var emailAccount = GetEmailAccountOfMessageTemplate(messageTemplate, languageId);

            //tokens
            var tokens = new List<Token>();
            _messageTokenProvider.AddStoreTokens(tokens, store, emailAccount);
            _messageTokenProvider.AddCustomerTokens(tokens, customer);


            //event notification
            _eventPublisher.MessageTokensAdded(messageTemplate, tokens);

            var toEmail = customer.Email;
            var toName = customer.GetFullName();
            return SendNotification(messageTemplate, emailAccount,
                languageId, tokens,
                toEmail, toName);
        }

        /// <summary>
        /// Sends password recovery message to a customer
        /// </summary>
        /// <param name="customer">Customer instance</param>
        /// <param name="languageId">Message language identifier</param>
        /// <returns>Queued email identifier</returns>
        public virtual int SendCustomerPasswordRecoveryMessage(Customer customer, int languageId)
        {
            if (customer == null)
                throw new ArgumentNullException("customer");

            var store = _storeContext.CurrentStore;
            languageId = EnsureLanguageIsActive(languageId, store.Id);

            var messageTemplate = GetActiveMessageTemplate("Customer.PasswordRecovery", store.Id);
            if (messageTemplate == null)
                return 0;

            //email account
            var emailAccount = GetEmailAccountOfMessageTemplate(messageTemplate, languageId);

            //tokens
            var tokens = new List<Token>();
            _messageTokenProvider.AddStoreTokens(tokens, store, emailAccount);
            _messageTokenProvider.AddCustomerTokens(tokens, customer);


            //event notification
            _eventPublisher.MessageTokensAdded(messageTemplate, tokens);

            var toEmail = customer.Email;
            var toName = customer.GetFullName();
            return SendNotification(messageTemplate, emailAccount,
                languageId, tokens,
                toEmail, toName);
        }

        #endregion

        #region Order workflow

        /// <summary>
        /// Sends an order placed notification to a vendor
        /// </summary>
        /// <param name="order">Order instance</param>
        /// <param name="vendor">Vendor instance</param>
        /// <param name="languageId">Message language identifier</param>
        /// <returns>Queued email identifier</returns>
        public virtual int SendOrderPlacedVendorNotification(Order order, Vendor vendor, int languageId)
        {
            if (order == null)
                throw new ArgumentNullException("order");

            if (vendor == null)
                throw new ArgumentNullException("vendor");

            var store = _storeService.GetStoreById(order.StoreId) ?? _storeContext.CurrentStore;
            languageId = EnsureLanguageIsActive(languageId, store.Id);

            var messageTemplate = GetActiveMessageTemplate("OrderPlaced.VendorNotification", store.Id);
            if (messageTemplate == null)
                return 0;

            //email account
            var emailAccount = GetEmailAccountOfMessageTemplate(messageTemplate, languageId);

            //tokens
            var tokens = new List<Token>();
            _messageTokenProvider.AddStoreTokens(tokens, store, emailAccount);
            _messageTokenProvider.AddOrderTokens(tokens, order, languageId, vendor.Id);
            _messageTokenProvider.AddCustomerTokens(tokens, order.Customer);

            //event notification
            _eventPublisher.MessageTokensAdded(messageTemplate, tokens);

            var toEmail = vendor.Email;
            var toName = vendor.Name;
            return SendNotification(messageTemplate, emailAccount,
                languageId, tokens,
                toEmail, toName);
        }

        /// <summary>
        /// Sends an order placed notification to a store owner
        /// </summary>
        /// <param name="order">Order instance</param>
        /// <param name="languageId">Message language identifier</param>
        /// <returns>Queued email identifier</returns>
        public virtual int SendOrderPlacedStoreOwnerNotification(Order order, int languageId)
        {
            if (order == null)
                throw new ArgumentNullException("order");

            var store = _storeService.GetStoreById(order.StoreId) ?? _storeContext.CurrentStore;
            languageId = EnsureLanguageIsActive(languageId, store.Id);

            var messageTemplate = GetActiveMessageTemplate("OrderPlaced.StoreOwnerNotification", store.Id);
            if (messageTemplate == null)
                return 0;

            //email account
            var emailAccount = GetEmailAccountOfMessageTemplate(messageTemplate, languageId);

            //tokens
            var tokens = new List<Token>();
            _messageTokenProvider.AddStoreTokens(tokens, store, emailAccount);
            _messageTokenProvider.AddOrderTokens(tokens, order, languageId);
            _messageTokenProvider.AddCustomerTokens(tokens, order.Customer);

            //event notification
            _eventPublisher.MessageTokensAdded(messageTemplate, tokens);

            var toEmail = emailAccount.Email;
            var toName = emailAccount.DisplayName;
            return SendNotification(messageTemplate, emailAccount,
                languageId, tokens,
                toEmail, toName);
        }

        /// <summary>
        /// Sends an order paid notification to a store owner
        /// </summary>
        /// <param name="order">Order instance</param>
        /// <param name="languageId">Message language identifier</param>
        /// <returns>Queued email identifier</returns>
        public virtual int SendOrderPaidStoreOwnerNotification(Order order, int languageId)
        {
            if (order == null)
                throw new ArgumentNullException("order");

            var store = _storeService.GetStoreById(order.StoreId) ?? _storeContext.CurrentStore;
            languageId = EnsureLanguageIsActive(languageId, store.Id);

            var messageTemplate = GetActiveMessageTemplate("OrderPaid.StoreOwnerNotification", store.Id);
            if (messageTemplate == null)
                return 0;

            //email account
            var emailAccount = GetEmailAccountOfMessageTemplate(messageTemplate, languageId);

            //tokens
            var tokens = new List<Token>();
            _messageTokenProvider.AddStoreTokens(tokens, store, emailAccount);
            _messageTokenProvider.AddOrderTokens(tokens, order, languageId);
            _messageTokenProvider.AddCustomerTokens(tokens, order.Customer);

            //event notification
            _eventPublisher.MessageTokensAdded(messageTemplate, tokens);

            var toEmail = emailAccount.Email;
            var toName = emailAccount.DisplayName;
            return SendNotification(messageTemplate, emailAccount,
                languageId, tokens,
                toEmail, toName);
        }

        /// <summary>
        /// Sends an order paid notification to a customer
        /// </summary>
        /// <param name="order">Order instance</param>
        /// <param name="languageId">Message language identifier</param>
        /// <param name="attachmentFilePath">Attachment file path</param>
        /// <param name="attachmentFileName">Attachment file name. If specified, then this file name will be sent to a recipient. Otherwise, "AttachmentFilePath" name will be used.</param>
        /// <returns>Queued email identifier</returns>
        public virtual int SendOrderPaidCustomerNotification(Order order, int languageId,
            string attachmentFilePath = null, string attachmentFileName = null)
        {
            if (order == null)
                throw new ArgumentNullException("order");

            var store = _storeService.GetStoreById(order.StoreId) ?? _storeContext.CurrentStore;
            languageId = EnsureLanguageIsActive(languageId, store.Id);

            var messageTemplate = GetActiveMessageTemplate("OrderPaid.CustomerNotification", store.Id);
            if (messageTemplate == null)
                return 0;

            //email account
            var emailAccount = GetEmailAccountOfMessageTemplate(messageTemplate, languageId);

            //tokens
            var tokens = new List<Token>();
            _messageTokenProvider.AddStoreTokens(tokens, store, emailAccount);
            _messageTokenProvider.AddOrderTokens(tokens, order, languageId);
            _messageTokenProvider.AddCustomerTokens(tokens, order.Customer);

            //event notification
            _eventPublisher.MessageTokensAdded(messageTemplate, tokens);

            var toEmail = order.BillingAddress.Email;
            var toName = string.Format("{0} {1}", order.BillingAddress.FirstName, order.BillingAddress.LastName);
            return SendNotification(messageTemplate, emailAccount,
                languageId, tokens,
                toEmail, toName,
                attachmentFilePath,
                attachmentFileName);
        }

        /// <summary>
        /// Sends an order paid notification to a vendor
        /// </summary>
        /// <param name="order">Order instance</param>
        /// <param name="vendor">Vendor instance</param>
        /// <param name="languageId">Message language identifier</param>
        /// <returns>Queued email identifier</returns>
        public virtual int SendOrderPaidVendorNotification(Order order, Vendor vendor, int languageId)
        {
            if (order == null)
                throw new ArgumentNullException("order");

            if (vendor == null)
                throw new ArgumentNullException("vendor");

            var store = _storeService.GetStoreById(order.StoreId) ?? _storeContext.CurrentStore;
            languageId = EnsureLanguageIsActive(languageId, store.Id);

            var messageTemplate = GetActiveMessageTemplate("OrderPaid.VendorNotification", store.Id);
            if (messageTemplate == null)
                return 0;

            //email account
            var emailAccount = GetEmailAccountOfMessageTemplate(messageTemplate, languageId);

            //tokens
            var tokens = new List<Token>();
            _messageTokenProvider.AddStoreTokens(tokens, store, emailAccount);
            _messageTokenProvider.AddOrderTokens(tokens, order, languageId, vendor.Id);
            _messageTokenProvider.AddCustomerTokens(tokens, order.Customer);

            //event notification
            _eventPublisher.MessageTokensAdded(messageTemplate, tokens);

            var toEmail = vendor.Email;
            var toName = vendor.Name;
            return SendNotification(messageTemplate, emailAccount,
                languageId, tokens,
                toEmail, toName);
        }

        /// <summary>
        /// Sends an order placed notification to a customer
        /// </summary>
        /// <param name="order">Order instance</param>
        /// <param name="languageId">Message language identifier</param>
        /// <param name="attachmentFilePath">Attachment file path</param>
        /// <param name="attachmentFileName">Attachment file name. If specified, then this file name will be sent to a recipient. Otherwise, "AttachmentFilePath" name will be used.</param>
        /// <returns>Queued email identifier</returns>
        public virtual int SendOrderPlacedCustomerNotification(Order order, int languageId,
            string attachmentFilePath = null, string attachmentFileName = null)
        {
            if (order == null)
                throw new ArgumentNullException("order");

            var store = _storeService.GetStoreById(order.StoreId) ?? _storeContext.CurrentStore;
            languageId = EnsureLanguageIsActive(languageId, store.Id);

            var messageTemplate = GetActiveMessageTemplate("OrderPlaced.CustomerNotification", store.Id);
            if (messageTemplate == null)
                return 0;

            //email account
            var emailAccount = GetEmailAccountOfMessageTemplate(messageTemplate, languageId);

            //tokens
            var tokens = new List<Token>();
            _messageTokenProvider.AddStoreTokens(tokens, store, emailAccount);
            _messageTokenProvider.AddOrderTokens(tokens, order, languageId);
            _messageTokenProvider.AddCustomerTokens(tokens, order.Customer);

            //event notification
            _eventPublisher.MessageTokensAdded(messageTemplate, tokens);

            var toEmail = order.BillingAddress.Email;
            var toName = string.Format("{0} {1}", order.BillingAddress.FirstName, order.BillingAddress.LastName);
            return SendNotification(messageTemplate, emailAccount,
                languageId, tokens,
                toEmail, toName,
                attachmentFilePath,
                attachmentFileName);
        }

        /// <summary>
        /// Sends a shipment sent notification to a customer
        /// </summary>
        /// <param name="shipment">Shipment</param>
        /// <param name="languageId">Message language identifier</param>
        /// <returns>Queued email identifier</returns>
        public virtual int SendShipmentSentCustomerNotification(Shipment shipment, int languageId)
        {
            if (shipment == null)
                throw new ArgumentNullException("shipment");

            var order = shipment.Order;
            if (order == null)
                throw new Exception("Order cannot be loaded");

            var store = _storeService.GetStoreById(order.StoreId) ?? _storeContext.CurrentStore;
            languageId = EnsureLanguageIsActive(languageId, store.Id);

            var messageTemplate = GetActiveMessageTemplate("ShipmentSent.CustomerNotification", store.Id);
            if (messageTemplate == null)
                return 0;

            //email account
            var emailAccount = GetEmailAccountOfMessageTemplate(messageTemplate, languageId);

            //tokens
            var tokens = new List<Token>();
            _messageTokenProvider.AddStoreTokens(tokens, store, emailAccount);
            _messageTokenProvider.AddShipmentTokens(tokens, shipment, languageId);
            _messageTokenProvider.AddOrderTokens(tokens, shipment.Order, languageId);
            _messageTokenProvider.AddCustomerTokens(tokens, shipment.Order.Customer);
            
            //event notification
            _eventPublisher.MessageTokensAdded(messageTemplate, tokens);

            var toEmail = order.BillingAddress.Email;
            var toName = string.Format("{0} {1}", order.BillingAddress.FirstName, order.BillingAddress.LastName);
            return SendNotification(messageTemplate, emailAccount,
                languageId, tokens,
                toEmail, toName);
        }

        /// <summary>
        /// Sends a shipment delivered notification to a customer
        /// </summary>
        /// <param name="shipment">Shipment</param>
        /// <param name="languageId">Message language identifier</param>
        /// <returns>Queued email identifier</returns>
        public virtual int SendShipmentDeliveredCustomerNotification(Shipment shipment, int languageId)
        {
            if (shipment == null)
                throw new ArgumentNullException("shipment");

            var order = shipment.Order;
            if (order == null)
                throw new Exception("Order cannot be loaded");

            var store = _storeService.GetStoreById(order.StoreId) ?? _storeContext.CurrentStore;
            languageId = EnsureLanguageIsActive(languageId, store.Id);

            var messageTemplate = GetActiveMessageTemplate("ShipmentDelivered.CustomerNotification", store.Id);
            if (messageTemplate == null)
                return 0;

            //email account
            var emailAccount = GetEmailAccountOfMessageTemplate(messageTemplate, languageId);

            //tokens
            var tokens = new List<Token>();
            _messageTokenProvider.AddStoreTokens(tokens, store, emailAccount);
            _messageTokenProvider.AddShipmentTokens(tokens, shipment, languageId);
            _messageTokenProvider.AddOrderTokens(tokens, shipment.Order, languageId);
            _messageTokenProvider.AddCustomerTokens(tokens, shipment.Order.Customer);

            //event notification
            _eventPublisher.MessageTokensAdded(messageTemplate, tokens);

            var toEmail = order.BillingAddress.Email;
            var toName = string.Format("{0} {1}", order.BillingAddress.FirstName, order.BillingAddress.LastName);
            return SendNotification(messageTemplate, emailAccount,
                languageId, tokens,
                toEmail, toName);
        }

        /// <summary>
        /// Sends an order completed notification to a customer
        /// </summary>
        /// <param name="order">Order instance</param>
        /// <param name="languageId">Message language identifier</param>
        /// <param name="attachmentFilePath">Attachment file path</param>
        /// <param name="attachmentFileName">Attachment file name. If specified, then this file name will be sent to a recipient. Otherwise, "AttachmentFilePath" name will be used.</param>
        /// <returns>Queued email identifier</returns>
        public virtual int SendOrderCompletedCustomerNotification(Order order, int languageId,
            string attachmentFilePath = null, string attachmentFileName = null)
        {
            if (order == null)
                throw new ArgumentNullException("order");

            var store = _storeService.GetStoreById(order.StoreId) ?? _storeContext.CurrentStore;
            languageId = EnsureLanguageIsActive(languageId, store.Id);

            var messageTemplate = GetActiveMessageTemplate("OrderCompleted.CustomerNotification", store.Id);
            if (messageTemplate == null)
                return 0;

            //email account
            var emailAccount = GetEmailAccountOfMessageTemplate(messageTemplate, languageId);

            //tokens
            var tokens = new List<Token>();
            _messageTokenProvider.AddStoreTokens(tokens, store, emailAccount);
            _messageTokenProvider.AddOrderTokens(tokens, order, languageId);
            _messageTokenProvider.AddCustomerTokens(tokens, order.Customer);

            //event notification
            _eventPublisher.MessageTokensAdded(messageTemplate, tokens);

            var toEmail = order.BillingAddress.Email;
            var toName = string.Format("{0} {1}", order.BillingAddress.FirstName, order.BillingAddress.LastName);
            return SendNotification(messageTemplate, emailAccount,
                languageId, tokens,
                toEmail, toName,
                attachmentFilePath,
                attachmentFileName);
        }

        /// <summary>
        /// Sends an order cancelled notification to a customer
        /// </summary>
        /// <param name="order">Order instance</param>
        /// <param name="languageId">Message language identifier</param>
        /// <returns>Queued email identifier</returns>
        public virtual int SendOrderCancelledCustomerNotification(Order order, int languageId)
        {
            if (order == null)
                throw new ArgumentNullException("order");

            var store = _storeService.GetStoreById(order.StoreId) ?? _storeContext.CurrentStore;
            languageId = EnsureLanguageIsActive(languageId, store.Id);

            var messageTemplate = GetActiveMessageTemplate("OrderCancelled.CustomerNotification", store.Id);
            if (messageTemplate == null)
                return 0;

            //email account
            var emailAccount = GetEmailAccountOfMessageTemplate(messageTemplate, languageId);

            //tokens
            var tokens = new List<Token>();
            _messageTokenProvider.AddStoreTokens(tokens, store, emailAccount);
            _messageTokenProvider.AddOrderTokens(tokens, order, languageId);
            _messageTokenProvider.AddCustomerTokens(tokens, order.Customer);

            //event notification
            _eventPublisher.MessageTokensAdded(messageTemplate, tokens);

            var toEmail = order.BillingAddress.Email;
            var toName = string.Format("{0} {1}", order.BillingAddress.FirstName, order.BillingAddress.LastName);
            return SendNotification(messageTemplate, emailAccount,
                languageId, tokens,
                toEmail, toName);
        }

        /// <summary>
        /// Sends an order refunded notification to a store owner
        /// </summary>
        /// <param name="order">Order instance</param>
        /// <param name="refundedAmount">Amount refunded</param>
        /// <param name="languageId">Message language identifier</param>
        /// <returns>Queued email identifier</returns>
        public virtual int SendOrderRefundedStoreOwnerNotification(Order order, decimal refundedAmount, int languageId)
        {
            if (order == null)
                throw new ArgumentNullException("order");

            var store = _storeService.GetStoreById(order.StoreId) ?? _storeContext.CurrentStore;
            languageId = EnsureLanguageIsActive(languageId, store.Id);

            var messageTemplate = GetActiveMessageTemplate("OrderRefunded.StoreOwnerNotification", store.Id);
            if (messageTemplate == null)
                return 0;

            //email account
            var emailAccount = GetEmailAccountOfMessageTemplate(messageTemplate, languageId);

            //tokens
            var tokens = new List<Token>();
            _messageTokenProvider.AddStoreTokens(tokens, store, emailAccount);
            _messageTokenProvider.AddOrderTokens(tokens, order, languageId);
            _messageTokenProvider.AddOrderRefundedTokens(tokens, order, refundedAmount);
            _messageTokenProvider.AddCustomerTokens(tokens, order.Customer);

            //event notification
            _eventPublisher.MessageTokensAdded(messageTemplate, tokens);

            var toEmail = emailAccount.Email;
            var toName = emailAccount.DisplayName;
            return SendNotification(messageTemplate, emailAccount,
                languageId, tokens,
                toEmail, toName);
        }

        /// <summary>
        /// Sends an order refunded notification to a customer
        /// </summary>
        /// <param name="order">Order instance</param>
        /// <param name="refundedAmount">Amount refunded</param>
        /// <param name="languageId">Message language identifier</param>
        /// <returns>Queued email identifier</returns>
        public virtual int SendOrderRefundedCustomerNotification(Order order, decimal refundedAmount, int languageId)
        {
            if (order == null)
                throw new ArgumentNullException("order");

            var store = _storeService.GetStoreById(order.StoreId) ?? _storeContext.CurrentStore;
            languageId = EnsureLanguageIsActive(languageId, store.Id);

            var messageTemplate = GetActiveMessageTemplate("OrderRefunded.CustomerNotification", store.Id);
            if (messageTemplate == null)
                return 0;

            //email account
            var emailAccount = GetEmailAccountOfMessageTemplate(messageTemplate, languageId);

            //tokens
            var tokens = new List<Token>();
            _messageTokenProvider.AddStoreTokens(tokens, store, emailAccount);
            _messageTokenProvider.AddOrderTokens(tokens, order, languageId);
            _messageTokenProvider.AddOrderRefundedTokens(tokens, order, refundedAmount);
            _messageTokenProvider.AddCustomerTokens(tokens, order.Customer);

            //event notification
            _eventPublisher.MessageTokensAdded(messageTemplate, tokens);

            var toEmail = order.BillingAddress.Email;
            var toName = string.Format("{0} {1}", order.BillingAddress.FirstName, order.BillingAddress.LastName);
            return SendNotification(messageTemplate, emailAccount,
                languageId, tokens,
                toEmail, toName);
        }

        /// <summary>
        /// Sends a new order note added notification to a customer
        /// </summary>
        /// <param name="orderNote">Order note</param>
        /// <param name="languageId">Message language identifier</param>
        /// <returns>Queued email identifier</returns>
        public virtual int SendNewOrderNoteAddedCustomerNotification(OrderNote orderNote, int languageId)
        {
            if (orderNote == null)
                throw new ArgumentNullException("orderNote");
           
            var order = orderNote.Order;

            var store = _storeService.GetStoreById(order.StoreId) ?? _storeContext.CurrentStore;
            languageId = EnsureLanguageIsActive(languageId, store.Id);

            var messageTemplate = GetActiveMessageTemplate("Customer.NewOrderNote", store.Id);
            if (messageTemplate == null)
                return 0;

            //email account
            var emailAccount = GetEmailAccountOfMessageTemplate(messageTemplate, languageId);

            //tokens
            var tokens = new List<Token>();
            _messageTokenProvider.AddStoreTokens(tokens, store, emailAccount);
            _messageTokenProvider.AddOrderNoteTokens(tokens, orderNote);
            _messageTokenProvider.AddOrderTokens(tokens, orderNote.Order, languageId);
            _messageTokenProvider.AddCustomerTokens(tokens, orderNote.Order.Customer);
            
            //event notification
            _eventPublisher.MessageTokensAdded(messageTemplate, tokens);

            var toEmail = order.BillingAddress.Email;
            var toName = string.Format("{0} {1}", order.BillingAddress.FirstName, order.BillingAddress.LastName);
            return SendNotification(messageTemplate, emailAccount,
                languageId, tokens,
                toEmail, toName);
        }

        /// <summary>
        /// Sends a "Recurring payment cancelled" notification to a store owner
        /// </summary>
        /// <param name="recurringPayment">Recurring payment</param>
        /// <param name="languageId">Message language identifier</param>
        /// <returns>Queued email identifier</returns>
        public virtual int SendRecurringPaymentCancelledStoreOwnerNotification(RecurringPayment recurringPayment, int languageId)
        {
            if (recurringPayment == null)
                throw new ArgumentNullException("recurringPayment");

            var store = _storeService.GetStoreById(recurringPayment.InitialOrder.StoreId) ?? _storeContext.CurrentStore;
            languageId = EnsureLanguageIsActive(languageId, store.Id);

            var messageTemplate = GetActiveMessageTemplate("RecurringPaymentCancelled.StoreOwnerNotification", store.Id);
            if (messageTemplate == null)
                return 0;

            //email account
            var emailAccount = GetEmailAccountOfMessageTemplate(messageTemplate, languageId);

            //tokens
            var tokens = new List<Token>();
            _messageTokenProvider.AddStoreTokens(tokens, store, emailAccount);
            _messageTokenProvider.AddOrderTokens(tokens, recurringPayment.InitialOrder, languageId);
            _messageTokenProvider.AddCustomerTokens(tokens, recurringPayment.InitialOrder.Customer);
            _messageTokenProvider.AddRecurringPaymentTokens(tokens, recurringPayment);

            //event notification
            _eventPublisher.MessageTokensAdded(messageTemplate, tokens);

            var toEmail = emailAccount.Email;
            var toName = emailAccount.DisplayName;
            return SendNotification(messageTemplate, emailAccount,
                languageId, tokens,
                toEmail, toName);
        }
        public int SendPOSupplierNotification(PurchaseOrder po, POTemplateMessage poTemplate, List<OrderItem> orderItems, Order order, Vendor vendor, int languageId, IProductService productService = null)
        {
            var store = _storeContext.CurrentStore;
            languageId = EnsureLanguageIsActive(languageId, store.Id);
            //email account
            var emailAccount = _emailAccountService.GetEmailAccountById(_emailAccountSettings.DefaultEmailAccountId);
            if (emailAccount == null)
                emailAccount = _emailAccountService.GetAllEmailAccounts().FirstOrDefault();
            if (emailAccount == null)
                return 0;
            //1. Build Html Body
            var messageHtmlTemplate = new MessageTemplate()
            {
                Body = poTemplate.POTemplateHtml,
                IsActive = true,
                Subject = poTemplate.Subject,
                BccEmailAddresses = poTemplate.BCC
            };
            var strStart = @"<!--PO.StartRepeatation-->";
            var strEnd = @"<!--PO.EndRepeatation-->";
            int startIndex = messageHtmlTemplate.Body.IndexOf(strStart);
            int endIndex = messageHtmlTemplate.Body.IndexOf(strEnd) + strEnd.Length;
            if (endIndex < startIndex)
                return 0;
            var originalDetailTemplate = messageHtmlTemplate.Body.Substring(startIndex, endIndex - startIndex);
            var replaceDetailTemplate = originalDetailTemplate.Replace(strStart, "").Replace(strEnd, "");
            replaceDetailTemplate = this.GetHtmlPODetail(replaceDetailTemplate, orderItems, productService);
            messageHtmlTemplate.Body = messageHtmlTemplate.Body.Replace(originalDetailTemplate, replaceDetailTemplate);
            //tokens
            var tokens = new List<Token>();
            _messageTokenProvider.AddStoreTokens(tokens, store, emailAccount);
            _messageTokenProvider.AddCustomerTokens(tokens, order.Customer);
            _messageTokenProvider.AddOrderTokens(tokens, order, languageId);
            _messageTokenProvider.AddPurchaseOrderTokens(tokens, po, languageId);
            //event notification
            _eventPublisher.MessageTokensAdded(messageHtmlTemplate, tokens);

            var toEmail = vendor.Email;
            var toName = vendor.Name;
           
            //2. Build Excel Body
            //3. Attach excel to email
            //4. Send mail
            var result = SendNotification(messageHtmlTemplate, emailAccount,
               languageId, tokens,
               toEmail, toName);
            po.Content = messageHtmlTemplate.Body;
            return result;
        }
        private string GetHtmlPODetail(string replaceDetailTemplate, List<OrderItem> orderItems, IProductService _productService)
        {
            var result = string.Empty;
            var poDetails = orderItems;
            var serapator = "<br/>";
            //get all parameter %Item.XXX% in template
            var regex = new Regex("%Item.(.*?)%");
            Match match;
            var listMatch = new List<TempMatch>();
            for (match = regex.Match(replaceDetailTemplate); match.Success; match = match.NextMatch())
            {
                listMatch.Add(new TempMatch() { Name = match.Value, Value = match.Groups[1].Value });
            }
            var lineNumber = 0;
            for (int i = 0; i < poDetails.Count; i++)
            {
                
                if (poDetails[i].Product.ProductType == ProductType.CompleteRodSets && _productService != null)
                {
                    var attributes = _productAttributeParser.ParseProductAttributeMappings(poDetails[i].AttributesXml);
                    var colorName = string.Empty;
                    for (int j = 0; j < attributes.Count; j++)
                    {
                        var attribute = attributes[j];
                        var valuesStr = _productAttributeParser.ParseValues(poDetails[i].AttributesXml, attribute.Id);
                        if (attribute.ProductAttribute.Name.Trim().ToLower() == "color")
                        {
                            for (int k = 0; k < valuesStr.Count; k++)
                            {
                                string valueStr = valuesStr[k];
                                int attributeValueId;
                                if (int.TryParse(valueStr, out attributeValueId))
                                {
                                    var attributeValue = _productAttributeService.GetProductAttributeValueById(attributeValueId);
                                    if (attributeValue != null)
                                    {
                                        colorName = attributeValue.Name;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                    //seperate to finial, pole, bracket, ring
                    //finial
                    if (poDetails[i].Product.FinialProductId.HasValue)
                    {
                        var finial = _productService.GetProductById(poDetails[i].Product.FinialProductId.Value);
                        if (finial != null)
                        {
                            lineNumber++;
                            var finialDetail = replaceDetailTemplate;
                            foreach (var item in listMatch)
                            {
                                var replaceValue = string.Empty;
                                switch (item.Name)
                                {
                                    case "%Item.Info.LineNumber%":
                                        replaceValue = lineNumber.ToString();
                                        break;
                                    case "%Item.Info.Name%":
                                        replaceValue = String.IsNullOrEmpty(finial.NameInPO) ? finial.Name : finial.NameInPO;
                                        break;
                                    case "%Item.Info.Packaged%":
                                        replaceValue = finial.Packaged;
                                        break;
                                    case "%Item.Info.Qty%":
                                        replaceValue = (poDetails[i].Quantity * poDetails[i].Product.NumberOfFinials).ToString();
                                        break;                          
                                    case "%Item.Color%":
                                        if (!string.IsNullOrEmpty(colorName))
                                        {
                                            var finialColorAttr = _productAttributeService.GetProductAttributeMappingsByProductId(finial.Id)
                                                .Where(x => x.ProductAttribute.Name.ToLower().Trim() == "color").FirstOrDefault();
                                            if (finialColorAttr != null)
                                            {
                                                var selectedColorFinial = finialColorAttr.ProductAttributeValues
                                                    .Where(x => x.Name.ToLower().Trim() == colorName.ToLower().Trim()).FirstOrDefault();
                                                if (selectedColorFinial != null)
                                                {
                                                    replaceValue += String.IsNullOrEmpty(selectedColorFinial.SupplierAttributeCode) ? selectedColorFinial.Name : selectedColorFinial.SupplierAttributeCode;
                                                }
                                                else
                                                {
                                                    replaceValue = colorName;
                                                }
                                            }
                                            else
                                            {
                                                replaceValue = colorName;
                                            }
                                        }
                                        break;
                                    case "%Item.Color[Part#]%":
                                        if (!string.IsNullOrEmpty(colorName))
                                        {
                                            var finialColorAttr = _productAttributeService.GetProductAttributeMappingsByProductId(finial.Id)
                                                .Where(x => x.ProductAttribute.Name.ToLower().Trim() == "color").FirstOrDefault();
                                            if (finialColorAttr != null)
                                            {
                                                var selectedColorFinial = finialColorAttr.ProductAttributeValues
                                                    .Where(x => x.Name.ToLower().Trim() == colorName.ToLower().Trim()).FirstOrDefault();
                                                if (selectedColorFinial != null)
                                                {
                                                    replaceValue = selectedColorFinial.SupplierPartNumber;
                                                }                                                
                                            }                                          
                                        }
                                        break;                                   
                                    default:
                                        replaceValue = string.Empty;
                                        break;
                                }
                                finialDetail = finialDetail.Replace(item.Name, replaceValue);
                            }
                            result += finialDetail;
                        }
                    }
                    
                    //pole
                    if (poDetails[i].Product.PoleProductId.HasValue)
                    {
                        var poles = _productService.GetProductById(poDetails[i].Product.PoleProductId.Value);
                        if (poles != null)
                        {
                            lineNumber++;
                            var poleDetail = replaceDetailTemplate;
                            foreach (var item in listMatch)
                            {
                                var replaceValue = string.Empty;
                                switch (item.Name)
                                {
                                    case "%Item.Info.LineNumber%":
                                        replaceValue = lineNumber.ToString();
                                        break;
                                    case "%Item.Info.Name%":
                                        replaceValue = String.IsNullOrEmpty(poles.NameInPO) ? poles.Name : poles.NameInPO;
                                        break;
                                    case "%Item.Info.Packaged%":
                                        replaceValue = poles.Packaged;
                                        break;
                                    case "%Item.Info.Qty%":
                                        replaceValue = (poDetails[i].Quantity * poDetails[i].Product.NumberOfPoles).ToString();
                                        break;
                                    case "%Item.Color%":
                                        if (!string.IsNullOrEmpty(colorName))
                                        {
                                            var poleColorAttr = _productAttributeService.GetProductAttributeMappingsByProductId(poles.Id)
                                                .Where(x => x.ProductAttribute.Name.ToLower().Trim() == "color").FirstOrDefault();
                                            if (poleColorAttr != null)
                                            {
                                                var selectedColorPole = poleColorAttr.ProductAttributeValues
                                                    .Where(x => x.Name.ToLower().Trim() == colorName.ToLower().Trim()).FirstOrDefault();
                                                if (selectedColorPole != null)
                                                {
                                                    replaceValue += String.IsNullOrEmpty(selectedColorPole.SupplierAttributeCode) ? selectedColorPole.Name : selectedColorPole.SupplierAttributeCode;
                                                }
                                                else
                                                {
                                                    replaceValue = colorName;
                                                }
                                            }
                                            else
                                            {
                                                replaceValue = colorName;
                                            }
                                        }
                                        break;
                                    case "%Item.Color[Part#]%":
                                        if (!string.IsNullOrEmpty(colorName))
                                        {
                                            var poleColorAttr = _productAttributeService.GetProductAttributeMappingsByProductId(poles.Id)
                                                .Where(x => x.ProductAttribute.Name.ToLower().Trim() == "color").FirstOrDefault();
                                            if (poleColorAttr != null)
                                            {
                                                var selectedColorPole = poleColorAttr.ProductAttributeValues
                                                    .Where(x => x.Name.ToLower().Trim() == colorName.ToLower().Trim()).FirstOrDefault();
                                                if (selectedColorPole != null)
                                                {
                                                    replaceValue = selectedColorPole.SupplierPartNumber;
                                                }
                                            }
                                        }
                                        break;
                                    default:
                                        replaceValue = string.Empty;
                                        break;
                                }
                                poleDetail = poleDetail.Replace(item.Name, replaceValue);
                            }
                            result += poleDetail;
                        }
                    }
                    //bracket
                    for (int j = 0; j < attributes.Count; j++)
                    {
                        var attribute = attributes[j];                      
                        if (attribute.ProductAttribute.Name.Trim().ToLower() == "brackets")
                        {
                            var valuesStr = _productAttributeParser.ParseValues(poDetails[i].AttributesXml, attribute.Id);
                            var valueStr = valuesStr.FirstOrDefault();
                            int attributeValueId;
                            if (valuesStr != null && int.TryParse(valueStr, out attributeValueId))
                            {
                                var attributeValue = _productAttributeService.GetProductAttributeValueById(attributeValueId);
                                if (attributeValue.ComponentProductId.HasValue)
                                {
                                    var bracket = _productService.GetProductById(attributeValue.ComponentProductId.Value);
                                    if (bracket != null)
                                    {
                                        lineNumber++;
                                        var bracketDetail = replaceDetailTemplate;
                                        foreach (var item in listMatch)
                                        {
                                            var replaceValue = string.Empty;
                                            switch (item.Name)
                                            {
                                                case "%Item.Info.LineNumber%":
                                                    replaceValue = lineNumber.ToString();
                                                    break;
                                                case "%Item.Info.Name%":
                                                    replaceValue = String.IsNullOrEmpty(bracket.NameInPO) ? bracket.Name : bracket.NameInPO;
                                                    break;
                                                case "%Item.Info.Packaged%":
                                                    replaceValue = bracket.Packaged;
                                                    break;
                                                case "%Item.Info.Qty%":
                                                    replaceValue = (poDetails[i].Quantity * attributeValue.Quantity).ToString();
                                                    break;
                                                case "%Item.Color%":
                                                    if (!string.IsNullOrEmpty(colorName))
                                                    {
                                                        var bracketColorAttr = _productAttributeService.GetProductAttributeMappingsByProductId(bracket.Id)
                                                            .Where(x => x.ProductAttribute.Name.ToLower().Trim() == "color").FirstOrDefault();
                                                        if (bracketColorAttr != null)
                                                        {
                                                            var selectedColorBracket = bracketColorAttr.ProductAttributeValues
                                                                .Where(x => x.Name.ToLower().Trim() == colorName.ToLower().Trim()).FirstOrDefault();
                                                            if (selectedColorBracket != null)
                                                            {
                                                                replaceValue += String.IsNullOrEmpty(selectedColorBracket.SupplierAttributeCode) ? selectedColorBracket.Name : selectedColorBracket.SupplierAttributeCode;
                                                            }
                                                            else
                                                            {
                                                                replaceValue = colorName;
                                                            }
                                                        }
                                                        else
                                                        {
                                                            replaceValue = colorName;
                                                        }
                                                    }
                                                    break;
                                                case "%Item.Color[Part#]%":
                                                    if (!string.IsNullOrEmpty(colorName))
                                                    {
                                                        var bracketColorAttr = _productAttributeService.GetProductAttributeMappingsByProductId(bracket.Id)
                                                            .Where(x => x.ProductAttribute.Name.ToLower().Trim() == "color").FirstOrDefault();
                                                        if (bracketColorAttr != null)
                                                        {
                                                            var selectedColorBracket = bracketColorAttr.ProductAttributeValues
                                                                .Where(x => x.Name.ToLower().Trim() == colorName.ToLower().Trim()).FirstOrDefault();
                                                            if (selectedColorBracket != null)
                                                            {
                                                                replaceValue = selectedColorBracket.SupplierPartNumber;
                                                            }
                                                        }
                                                    }
                                                    break;
                                                default:
                                                    replaceValue = string.Empty;
                                                    break;
                                            }
                                            bracketDetail = bracketDetail.Replace(item.Name, replaceValue);
                                        }
                                        result += bracketDetail;

                                    }
                                }
                            }
                            
                        }
                    }
                    //ring
                    for (int j = 0; j < attributes.Count; j++)
                    {
                        var attribute = attributes[j];
                        if (attribute.ProductAttribute.Name.Trim().ToLower() == "ring quantity")
                        {
                            var valuesStr = _productAttributeParser.ParseValues(poDetails[i].AttributesXml, attribute.Id);
                            var valueStr = valuesStr.FirstOrDefault();
                            int attributeValueId;
                            if (valuesStr != null && int.TryParse(valueStr, out attributeValueId))
                            {
                                var attributeValue = _productAttributeService.GetProductAttributeValueById(attributeValueId);
                                if (attributeValue.ComponentProductId.HasValue)
                                {
                                    var ring = _productService.GetProductById(attributeValue.ComponentProductId.Value);
                                    if (ring != null)
                                    {
                                        lineNumber++;
                                        var ringDetail = replaceDetailTemplate;
                                        foreach (var item in listMatch)
                                        {
                                            var replaceValue = string.Empty;
                                            switch (item.Name)
                                            {
                                                case "%Item.Info.LineNumber%":
                                                    replaceValue = lineNumber.ToString();
                                                    break;
                                                case "%Item.Info.Name%":
                                                    replaceValue = String.IsNullOrEmpty(ring.NameInPO) ? ring.Name : ring.NameInPO;
                                                    break;
                                                case "%Item.Info.Packaged%":
                                                    replaceValue = ring.Packaged;
                                                    break;
                                                case "%Item.Info.Qty%":
                                                    replaceValue = (poDetails[i].Quantity * attributeValue.Quantity).ToString();
                                                    break;
                                                case "%Item.Color%":
                                                    if (!string.IsNullOrEmpty(colorName))
                                                    {
                                                        var ringColorAttr = _productAttributeService.GetProductAttributeMappingsByProductId(ring.Id)
                                                            .Where(x => x.ProductAttribute.Name.ToLower().Trim() == "color").FirstOrDefault();
                                                        if (ringColorAttr != null)
                                                        {
                                                            var selectedColorRing = ringColorAttr.ProductAttributeValues
                                                                .Where(x => x.Name.ToLower().Trim() == colorName.ToLower().Trim()).FirstOrDefault();
                                                            if (selectedColorRing != null)
                                                            {
                                                                replaceValue += String.IsNullOrEmpty(selectedColorRing.SupplierAttributeCode) ? selectedColorRing.Name : selectedColorRing.SupplierAttributeCode;
                                                            }
                                                            else
                                                            {
                                                                replaceValue = colorName;
                                                            }
                                                        }
                                                        else
                                                        {
                                                            replaceValue = colorName;
                                                        }
                                                    }
                                                    break;
                                                case "%Item.Color[Part#]%":
                                                    if (!string.IsNullOrEmpty(colorName))
                                                    {
                                                        var ringColorAttr = _productAttributeService.GetProductAttributeMappingsByProductId(ring.Id)
                                                            .Where(x => x.ProductAttribute.Name.ToLower().Trim() == "color").FirstOrDefault();
                                                        if (ringColorAttr != null)
                                                        {
                                                            var selectedColorRing = ringColorAttr.ProductAttributeValues
                                                                .Where(x => x.Name.ToLower().Trim() == colorName.ToLower().Trim()).FirstOrDefault();
                                                            if (selectedColorRing != null)
                                                            {
                                                                replaceValue = selectedColorRing.SupplierPartNumber;
                                                            }
                                                        }
                                                    }
                                                    break;
                                                default:
                                                    replaceValue = string.Empty;
                                                    break;
                                            }
                                            ringDetail = ringDetail.Replace(item.Name, replaceValue);
                                        }
                                        result += ringDetail;

                                    }
                                }
                            }

                        }
                    }

                }
                else
                {
                    lineNumber++;
                    var singleDetail = replaceDetailTemplate;
                    foreach (var item in listMatch)
                    {
                        var replaceValue = string.Empty;
                        switch (item.Name)
                        {
                            case "%Item.Info.LineNumber%":
                                replaceValue = lineNumber.ToString();
                                break;
                            case "%Item.Info.Name%":
                                replaceValue = String.IsNullOrEmpty(poDetails[i].Product.NameInPO) ? poDetails[i].Product.Name : poDetails[i].Product.NameInPO;
                                break;
                            case "%Item.Info.Packaged%":
                                replaceValue = poDetails[i].Product.Packaged;
                                break;
                            case "%Item.Info.Qty%":
                                replaceValue = poDetails[i].Quantity.ToString();
                                break;
                            case "%Item.Info.FullRodWidth%":
                                replaceValue = poDetails[i].FullRodWidth;
                                break;
                            default: // attributes format value = [Attribute Name].[Attribute Value]
                                var arrTemp = item.Value.Split('.');
                                var attributes = _productAttributeParser.ParseProductAttributeMappings(poDetails[i].AttributesXml);
                                //1.get attribute has Name = item.Value
                                for (int j = 0; j < attributes.Count; j++)
                                {
                                    var attribute = attributes[j];
                                    var valuesStr = _productAttributeParser.ParseValues(poDetails[i].AttributesXml, attribute.Id);
                                    //2.get selected value 
                                    //check case: item.Value[xxx]
                                    var arrNameProp = arrTemp[0].ToLower().Trim().Split('[');
                                    if ((arrTemp[0].ToLower().Trim() == attribute.ProductAttribute.Name.Replace(" ", "").ToLower())
                                        || (arrNameProp.Length == 2 && arrNameProp[0].ToLower().Trim() == attribute.ProductAttribute.Name.Replace(" ", "").ToLower()))
                                    {
                                        if (arrTemp.Length == 1)
                                        {
                                            for (int k = 0; k < valuesStr.Count; k++)
                                            {
                                                string valueStr = valuesStr[k];
                                                if (!attribute.ShouldHaveValues())//textbox...
                                                {
                                                    replaceValue += HttpUtility.HtmlEncode(valueStr) + serapator;
                                                }
                                                else
                                                {
                                                    //attributes with values
                                                    int attributeValueId;
                                                    if (int.TryParse(valueStr, out attributeValueId))
                                                    {
                                                        var attributeValue = _productAttributeService.GetProductAttributeValueById(attributeValueId);
                                                        if (attributeValue != null)
                                                        {
                                                            if (arrNameProp.Length == 2)
                                                            {
                                                                switch (arrNameProp[1].TrimEnd(']').ToLower())
                                                                {
                                                                    case "part#":
                                                                        replaceValue += attributeValue.SupplierPartNumber;
                                                                        break;
                                                                    case "finialpartnumber":
                                                                        replaceValue += attributeValue.FinialPartNumber;
                                                                        break;
                                                                    case "polepartnumber":
                                                                        replaceValue += attributeValue.PolePartNumber;
                                                                        break;
                                                                    case "bracketpartnumber":
                                                                        replaceValue += attributeValue.BracketPartNumber;
                                                                        break;
                                                                    case "ringpartnumber":
                                                                        replaceValue += attributeValue.RingPartNumber;
                                                                        break;
                                                                    case "productquantity":
                                                                        replaceValue += attributeValue.Quantity;
                                                                        break;
                                                                    case "packaged":
                                                                        replaceValue += attributeValue.Packaged;
                                                                        break;
                                                                    default:
                                                                        break;
                                                                }
                                                            }
                                                            else
                                                            {
                                                                replaceValue += String.IsNullOrEmpty(attributeValue.SupplierAttributeCode) ? attributeValue.Name : attributeValue.SupplierAttributeCode;
                                                            }
                                                            replaceValue += serapator;
                                                        }
                                                    }

                                                }
                                            }
                                        }
                                        else
                                        {
                                            var attributeValueName = arrTemp[1];

                                            for (int k = 0; k < valuesStr.Count; k++)
                                            {
                                                string valueStr = valuesStr[k];
                                                //attributes with values
                                                int attributeValueId;
                                                if (int.TryParse(valueStr, out attributeValueId))
                                                {
                                                    var attributeValue = _productAttributeService.GetProductAttributeValueById(attributeValueId);
                                                    //Compare attribute name                         
                                                    if (attributeValue != null && attributeValue.Name.Replace(" ", "").ToLower() == attributeValueName.Trim().ToLower())
                                                    {
                                                        replaceValue += String.IsNullOrEmpty(attributeValue.SupplierAttributeCode) ? attributeValue.Name : attributeValue.SupplierAttributeCode;
                                                        replaceValue += serapator;
                                                    }
                                                }
                                            }
                                        }

                                        break;
                                    }
                                }

                                break;
                        }
                        singleDetail = singleDetail.Replace(item.Name, replaceValue);
                    }
                    result += singleDetail;
                }

            }
            return result;
        }
        private class TempMatch
        {
            public string Name { get; set; }
            public string Value { get; set; }
        }

        #endregion

        #region Newsletter workflow

        /// <summary>
        /// Sends a newsletter subscription activation message
        /// </summary>
        /// <param name="subscription">Newsletter subscription</param>
        /// <param name="languageId">Language identifier</param>
        /// <returns>Queued email identifier</returns>
        public virtual int SendNewsLetterSubscriptionActivationMessage(NewsLetterSubscription subscription,
            int languageId)
        {
            if (subscription == null)
                throw new ArgumentNullException("subscription");

            var store = _storeContext.CurrentStore;
            languageId = EnsureLanguageIsActive(languageId, store.Id);

            var messageTemplate = GetActiveMessageTemplate("NewsLetterSubscription.ActivationMessage", store.Id);
            if (messageTemplate == null)
                return 0;

            //email account
            var emailAccount = GetEmailAccountOfMessageTemplate(messageTemplate, languageId);

            //tokens
            var tokens = new List<Token>();
            _messageTokenProvider.AddStoreTokens(tokens, store, emailAccount);
            _messageTokenProvider.AddNewsLetterSubscriptionTokens(tokens, subscription);
            
            //event notification
            _eventPublisher.MessageTokensAdded(messageTemplate, tokens);

            var toEmail = subscription.Email;
            var toName = "";
            return SendNotification(messageTemplate, emailAccount,
                languageId, tokens,
                toEmail, toName);
        }

        /// <summary>
        /// Sends a newsletter subscription deactivation message
        /// </summary>
        /// <param name="subscription">Newsletter subscription</param>
        /// <param name="languageId">Language identifier</param>
        /// <returns>Queued email identifier</returns>
        public virtual int SendNewsLetterSubscriptionDeactivationMessage(NewsLetterSubscription subscription,
            int languageId)
        {
            if (subscription == null)
                throw new ArgumentNullException("subscription");

            var store = _storeContext.CurrentStore;
            languageId = EnsureLanguageIsActive(languageId, store.Id);

            var messageTemplate = GetActiveMessageTemplate("NewsLetterSubscription.DeactivationMessage", store.Id);
            if (messageTemplate == null)
                return 0;

            //email account
            var emailAccount = GetEmailAccountOfMessageTemplate(messageTemplate, languageId);

            //tokens
            var tokens = new List<Token>();
            _messageTokenProvider.AddStoreTokens(tokens, store, emailAccount);
            _messageTokenProvider.AddNewsLetterSubscriptionTokens(tokens, subscription);

            //event notification
            _eventPublisher.MessageTokensAdded(messageTemplate, tokens);

            var toEmail = subscription.Email;
            var toName = "";
            return SendNotification(messageTemplate, emailAccount,
                languageId, tokens,
                toEmail, toName);
        }

        #endregion
        
        #region Send a message to a friend

        /// <summary>
        /// Sends "email a friend" message
        /// </summary>
        /// <param name="customer">Customer instance</param>
        /// <param name="languageId">Message language identifier</param>
        /// <param name="product">Product instance</param>
        /// <param name="customerEmail">Customer's email</param>
        /// <param name="friendsEmail">Friend's email</param>
        /// <param name="personalMessage">Personal message</param>
        /// <returns>Queued email identifier</returns>
        public virtual int SendProductEmailAFriendMessage(Customer customer, int languageId,
            Product product, string customerEmail, string friendsEmail, string personalMessage)
        {
            if (customer == null)
                throw new ArgumentNullException("customer");

            if (product == null)
                throw new ArgumentNullException("product");

            var store = _storeContext.CurrentStore;
            languageId = EnsureLanguageIsActive(languageId, store.Id);

            var messageTemplate = GetActiveMessageTemplate("Service.EmailAFriend", store.Id);
            if (messageTemplate == null)
                return 0;

            //email account
            var emailAccount = GetEmailAccountOfMessageTemplate(messageTemplate, languageId);

            //tokens
            var tokens = new List<Token>();
            _messageTokenProvider.AddStoreTokens(tokens, store, emailAccount);
            _messageTokenProvider.AddCustomerTokens(tokens, customer);
            _messageTokenProvider.AddProductTokens(tokens, product, languageId);
            tokens.Add(new Token("EmailAFriend.PersonalMessage", personalMessage, true));
            tokens.Add(new Token("EmailAFriend.Email", customerEmail));

            //event notification
            _eventPublisher.MessageTokensAdded(messageTemplate, tokens);

            var toEmail = friendsEmail;
            var toName = "";
            return SendNotification(messageTemplate, emailAccount,
                languageId, tokens,
                toEmail, toName);
        }

        /// <summary>
        /// Sends wishlist "email a friend" message
        /// </summary>
        /// <param name="customer">Customer</param>
        /// <param name="languageId">Message language identifier</param>
        /// <param name="customerEmail">Customer's email</param>
        /// <param name="friendsEmail">Friend's email</param>
        /// <param name="personalMessage">Personal message</param>
        /// <returns>Queued email identifier</returns>
        public virtual int SendWishlistEmailAFriendMessage(Customer customer, int languageId,
             string customerEmail, string friendsEmail, string personalMessage)
        {
            if (customer == null)
                throw new ArgumentNullException("customer");

            var store = _storeContext.CurrentStore;
            languageId = EnsureLanguageIsActive(languageId, store.Id);

            var messageTemplate = GetActiveMessageTemplate("Wishlist.EmailAFriend", store.Id);
            if (messageTemplate == null)
                return 0;

            //email account
            var emailAccount = GetEmailAccountOfMessageTemplate(messageTemplate, languageId);

            //tokens
            var tokens = new List<Token>();
            _messageTokenProvider.AddStoreTokens(tokens, store, emailAccount);
            _messageTokenProvider.AddCustomerTokens(tokens, customer);
            tokens.Add(new Token("Wishlist.PersonalMessage", personalMessage, true));
            tokens.Add(new Token("Wishlist.Email", customerEmail));

            //event notification
            _eventPublisher.MessageTokensAdded(messageTemplate, tokens);

            var toEmail = friendsEmail;
            var toName = "";
            return SendNotification(messageTemplate, emailAccount,
                languageId, tokens,
                toEmail, toName);
        }

        #endregion

        #region Return requests

        /// <summary>
        /// Sends 'New Return Request' message to a store owner
        /// </summary>
        /// <param name="returnRequest">Return request</param>
        /// <param name="orderItem">Order item</param>
        /// <param name="languageId">Message language identifier</param>
        /// <returns>Queued email identifier</returns>
        public virtual int SendNewReturnRequestStoreOwnerNotification(ReturnRequest returnRequest, OrderItem orderItem, int languageId)
        {
            if (returnRequest == null)
                throw new ArgumentNullException("returnRequest");

            var store = _storeService.GetStoreById(orderItem.Order.StoreId) ?? _storeContext.CurrentStore;
            languageId = EnsureLanguageIsActive(languageId, store.Id);

            var messageTemplate = GetActiveMessageTemplate("NewReturnRequest.StoreOwnerNotification", store.Id);
            if (messageTemplate == null)
                return 0;

            //email account
            var emailAccount = GetEmailAccountOfMessageTemplate(messageTemplate, languageId);

            //tokens
            var tokens = new List<Token>();
            _messageTokenProvider.AddStoreTokens(tokens, store, emailAccount);
            _messageTokenProvider.AddCustomerTokens(tokens, returnRequest.Customer);
            _messageTokenProvider.AddReturnRequestTokens(tokens, returnRequest, orderItem);

            //event notification
            _eventPublisher.MessageTokensAdded(messageTemplate, tokens);

            var toEmail = emailAccount.Email;
            var toName = emailAccount.DisplayName;
            return SendNotification(messageTemplate, emailAccount,
                languageId, tokens,
                toEmail, toName);
        }

        /// <summary>
        /// Sends 'Return Request status changed' message to a customer
        /// </summary>
        /// <param name="returnRequest">Return request</param>
        /// <param name="orderItem">Order item</param>
        /// <param name="languageId">Message language identifier</param>
        /// <returns>Queued email identifier</returns>
        public virtual int SendReturnRequestStatusChangedCustomerNotification(ReturnRequest returnRequest, OrderItem orderItem, int languageId)
        {
            if (returnRequest == null)
                throw new ArgumentNullException("returnRequest");

            var store = _storeService.GetStoreById(orderItem.Order.StoreId) ?? _storeContext.CurrentStore;
            languageId = EnsureLanguageIsActive(languageId, store.Id);

            var messageTemplate = GetActiveMessageTemplate("ReturnRequestStatusChanged.CustomerNotification", store.Id);
            if (messageTemplate == null)
                return 0;

            //email account
            var emailAccount = GetEmailAccountOfMessageTemplate(messageTemplate, languageId);

            //tokens
            var tokens = new List<Token>();
            _messageTokenProvider.AddStoreTokens(tokens, store, emailAccount);
            _messageTokenProvider.AddCustomerTokens(tokens, returnRequest.Customer);
            _messageTokenProvider.AddReturnRequestTokens(tokens, returnRequest, orderItem);

            //event notification
            _eventPublisher.MessageTokensAdded(messageTemplate, tokens);

            string toEmail = returnRequest.Customer.IsGuest() ? 
                orderItem.Order.BillingAddress.Email :
                returnRequest.Customer.Email;
            var toName = returnRequest.Customer.IsGuest() ? 
                orderItem.Order.BillingAddress.FirstName :
                returnRequest.Customer.GetFullName();
            return SendNotification(messageTemplate, emailAccount,
                languageId, tokens,
                toEmail, toName);
        }
        
        #endregion

        #region Forum Notifications

        /// <summary>
        /// Sends a forum subscription message to a customer
        /// </summary>
        /// <param name="customer">Customer instance</param>
        /// <param name="forumTopic">Forum Topic</param>
        /// <param name="forum">Forum</param>
        /// <param name="languageId">Message language identifier</param>
        /// <returns>Queued email identifier</returns>
        public int SendNewForumTopicMessage(Customer customer,
            ForumTopic forumTopic, Forum forum, int languageId)
        {
            if (customer == null)
            {
                throw new ArgumentNullException("customer");
            }
            var store = _storeContext.CurrentStore;

            var messageTemplate = GetActiveMessageTemplate("Forums.NewForumTopic", store.Id);
            if (messageTemplate == null)
                return 0;

            //email account
            var emailAccount = GetEmailAccountOfMessageTemplate(messageTemplate, languageId);

            //tokens
            var tokens = new List<Token>();
            _messageTokenProvider.AddStoreTokens(tokens, store, emailAccount);
            _messageTokenProvider.AddForumTopicTokens(tokens, forumTopic);
            _messageTokenProvider.AddForumTokens(tokens, forumTopic.Forum);
            _messageTokenProvider.AddCustomerTokens(tokens, customer);

            //event notification
            _eventPublisher.MessageTokensAdded(messageTemplate, tokens);

            var toEmail = customer.Email;
            var toName = customer.GetFullName();

            return SendNotification(messageTemplate, emailAccount, languageId, tokens, toEmail, toName);
        }

        /// <summary>
        /// Sends a forum subscription message to a customer
        /// </summary>
        /// <param name="customer">Customer instance</param>
        /// <param name="forumPost">Forum post</param>
        /// <param name="forumTopic">Forum Topic</param>
        /// <param name="forum">Forum</param>
        /// <param name="friendlyForumTopicPageIndex">Friendly (starts with 1) forum topic page to use for URL generation</param>
        /// <param name="languageId">Message language identifier</param>
        /// <returns>Queued email identifier</returns>
        public int SendNewForumPostMessage(Customer customer,
            ForumPost forumPost, ForumTopic forumTopic,
            Forum forum, int friendlyForumTopicPageIndex, int languageId)
        {
            if (customer == null)
            {
                throw new ArgumentNullException("customer");
            }

            var store = _storeContext.CurrentStore;

            var messageTemplate = GetActiveMessageTemplate("Forums.NewForumPost", store.Id);
            if (messageTemplate == null )
            {
                return 0;
            }

            //email account
            var emailAccount = GetEmailAccountOfMessageTemplate(messageTemplate, languageId);

            //tokens
            var tokens = new List<Token>();
            _messageTokenProvider.AddStoreTokens(tokens, store, emailAccount);
            _messageTokenProvider.AddForumPostTokens(tokens, forumPost);
            _messageTokenProvider.AddForumTopicTokens(tokens, forumPost.ForumTopic,
                friendlyForumTopicPageIndex, forumPost.Id);
            _messageTokenProvider.AddForumTokens(tokens, forumPost.ForumTopic.Forum);
            _messageTokenProvider.AddCustomerTokens(tokens, customer);

            //event notification
            _eventPublisher.MessageTokensAdded(messageTemplate, tokens);
          
            var toEmail = customer.Email;
            var toName = customer.GetFullName();

            return SendNotification(messageTemplate, emailAccount, languageId, tokens, toEmail, toName);
        }

        /// <summary>
        /// Sends a private message notification
        /// </summary>
        /// <param name="privateMessage">Private message</param>
        /// <param name="languageId">Message language identifier</param>
        /// <returns>Queued email identifier</returns>
        public int SendPrivateMessageNotification(PrivateMessage privateMessage, int languageId)
        {
            if (privateMessage == null)
            {
                throw new ArgumentNullException("privateMessage");
            }

            var store = _storeService.GetStoreById(privateMessage.StoreId) ?? _storeContext.CurrentStore;

            var messageTemplate = GetActiveMessageTemplate("Customer.NewPM", store.Id);
            if (messageTemplate == null )
            {
                return 0;
            }

            //email account
            var emailAccount = GetEmailAccountOfMessageTemplate(messageTemplate, languageId);

            //tokens
            var tokens = new List<Token>();
            _messageTokenProvider.AddStoreTokens(tokens, store, emailAccount);
            _messageTokenProvider.AddPrivateMessageTokens(tokens, privateMessage);
            _messageTokenProvider.AddCustomerTokens(tokens, privateMessage.ToCustomer);

            //event notification
            _eventPublisher.MessageTokensAdded(messageTemplate, tokens);
           
            var toEmail = privateMessage.ToCustomer.Email;
            var toName = privateMessage.ToCustomer.GetFullName();

            return SendNotification(messageTemplate, emailAccount, languageId, tokens, toEmail, toName);
        }

        #endregion

        #region Misc

        /// <summary>
        /// Sends 'New vendor account submitted' message to a store owner
        /// </summary>
        /// <param name="customer">Customer</param>
        /// <param name="vendor">Vendor</param>
        /// <param name="languageId">Message language identifier</param>
        /// <returns>Queued email identifier</returns>
        public virtual int SendNewVendorAccountApplyStoreOwnerNotification(Customer customer, Vendor vendor, int languageId)
        {
            if (customer == null)
                throw new ArgumentNullException("customer");

            if (vendor == null)
                throw new ArgumentNullException("vendor");

            var store = _storeContext.CurrentStore;
            languageId = EnsureLanguageIsActive(languageId, store.Id);

            var messageTemplate = GetActiveMessageTemplate("VendorAccountApply.StoreOwnerNotification", store.Id);
            if (messageTemplate == null)
                return 0;

            //email account
            var emailAccount = GetEmailAccountOfMessageTemplate(messageTemplate, languageId);

            //tokens
            var tokens = new List<Token>();
            _messageTokenProvider.AddStoreTokens(tokens, store, emailAccount);
            _messageTokenProvider.AddCustomerTokens(tokens, customer);
            _messageTokenProvider.AddVendorTokens(tokens, vendor);

            //event notification
            _eventPublisher.MessageTokensAdded(messageTemplate, tokens);

            var toEmail = emailAccount.Email;
            var toName = emailAccount.DisplayName;
            return SendNotification(messageTemplate, emailAccount,
                languageId, tokens,
                toEmail, toName);
        }

        /// <summary>
        /// Sends a gift card notification
        /// </summary>
        /// <param name="giftCard">Gift card</param>
        /// <param name="languageId">Message language identifier</param>
        /// <returns>Queued email identifier</returns>
        public virtual int SendGiftCardNotification(GiftCard giftCard, int languageId)
        {
            if (giftCard == null)
                throw new ArgumentNullException("giftCard");

            Store store = null;
            var order = giftCard.PurchasedWithOrderItem != null ?
                giftCard.PurchasedWithOrderItem.Order : 
                null;
            if (order != null)
                store = _storeService.GetStoreById(order.StoreId);
            if (store == null)
                store = _storeContext.CurrentStore;

            languageId = EnsureLanguageIsActive(languageId, store.Id);

            var messageTemplate = GetActiveMessageTemplate("GiftCard.Notification", store.Id);
            if (messageTemplate == null)
                return 0;

            //email account
            var emailAccount = GetEmailAccountOfMessageTemplate(messageTemplate, languageId);

            //tokens
            var tokens = new List<Token>();
            _messageTokenProvider.AddStoreTokens(tokens, store, emailAccount);
            _messageTokenProvider.AddGiftCardTokens(tokens, giftCard);
            
            //event notification
            _eventPublisher.MessageTokensAdded(messageTemplate, tokens);
            var toEmail = giftCard.RecipientEmail;
            var toName = giftCard.RecipientName;
            return SendNotification(messageTemplate, emailAccount,
                languageId, tokens,
                toEmail, toName);
        }
        
        /// <summary>
        /// Sends a product review notification message to a store owner
        /// </summary>
        /// <param name="productReview">Product review</param>
        /// <param name="languageId">Message language identifier</param>
        /// <returns>Queued email identifier</returns>
        public virtual int SendProductReviewNotificationMessage(ProductReview productReview,
            int languageId)
        {
            if (productReview == null)
                throw new ArgumentNullException("productReview");

            var store = _storeContext.CurrentStore;
            languageId = EnsureLanguageIsActive(languageId, store.Id);

            var messageTemplate = GetActiveMessageTemplate("Product.ProductReview", store.Id);
            if (messageTemplate == null)
                return 0;

            //email account
            var emailAccount = GetEmailAccountOfMessageTemplate(messageTemplate, languageId);

            //tokens
            var tokens = new List<Token>();
            _messageTokenProvider.AddStoreTokens(tokens, store, emailAccount);
            _messageTokenProvider.AddProductReviewTokens(tokens, productReview);
            _messageTokenProvider.AddCustomerTokens(tokens, productReview.Customer);
            
            //event notification
            _eventPublisher.MessageTokensAdded(messageTemplate, tokens);

            var toEmail = emailAccount.Email;
            var toName = emailAccount.DisplayName;
            return SendNotification(messageTemplate, emailAccount,
                languageId, tokens,
                toEmail, toName);
        }

        /// <summary>
        /// Sends a "quantity below" notification to a store owner
        /// </summary>
        /// <param name="product">Product</param>
        /// <param name="languageId">Message language identifier</param>
        /// <returns>Queued email identifier</returns>
        public virtual int SendQuantityBelowStoreOwnerNotification(Product product,  int languageId)
        {
            if (product== null)
                throw new ArgumentNullException("product");

            var store = _storeContext.CurrentStore;
            languageId = EnsureLanguageIsActive(languageId, store.Id);

            var messageTemplate = GetActiveMessageTemplate("QuantityBelow.StoreOwnerNotification", store.Id);
            if (messageTemplate == null)
                return 0;

            //email account
            var emailAccount = GetEmailAccountOfMessageTemplate(messageTemplate, languageId);

            var tokens = new List<Token>();
            _messageTokenProvider.AddStoreTokens(tokens, store, emailAccount);
            _messageTokenProvider.AddProductTokens(tokens, product, languageId);

            //event notification
            _eventPublisher.MessageTokensAdded(messageTemplate, tokens);

            var toEmail = emailAccount.Email;
            var toName = emailAccount.DisplayName;
            return SendNotification(messageTemplate, emailAccount,
                languageId, tokens,
                toEmail, toName);
        }

        /// <summary>
        /// Sends a "quantity below" notification to a store owner
        /// </summary>
        /// <param name="combination">Attribute combination</param>
        /// <param name="languageId">Message language identifier</param>
        /// <returns>Queued email identifier</returns>
        public virtual int SendQuantityBelowStoreOwnerNotification(ProductAttributeCombination combination, int languageId)
        {
            if (combination == null)
                throw new ArgumentNullException("combination");

            var store = _storeContext.CurrentStore;
            languageId = EnsureLanguageIsActive(languageId, store.Id);

            var messageTemplate = GetActiveMessageTemplate("QuantityBelow.AttributeCombination.StoreOwnerNotification", store.Id);
            if (messageTemplate == null)
                return 0;

            //email account
            var emailAccount = GetEmailAccountOfMessageTemplate(messageTemplate, languageId);

            var product = combination.Product;

            var tokens = new List<Token>();
            _messageTokenProvider.AddStoreTokens(tokens, store, emailAccount);
            _messageTokenProvider.AddProductTokens(tokens, product, languageId);
            _messageTokenProvider.AddAttributeCombinationTokens(tokens, combination, languageId);

            //event notification
            _eventPublisher.MessageTokensAdded(messageTemplate, tokens);

            var toEmail = emailAccount.Email;
            var toName = emailAccount.DisplayName;
            return SendNotification(messageTemplate, emailAccount,
                languageId, tokens,
                toEmail, toName);
        }

        /// <summary>
        /// Sends a "new VAT sumitted" notification to a store owner
        /// </summary>
        /// <param name="customer">Customer</param>
        /// <param name="vatName">Received VAT name</param>
        /// <param name="vatAddress">Received VAT address</param>
        /// <param name="languageId">Message language identifier</param>
        /// <returns>Queued email identifier</returns>
        public virtual int SendNewVatSubmittedStoreOwnerNotification(Customer customer,
            string vatName, string vatAddress, int languageId)
        {
            if (customer == null)
                throw new ArgumentNullException("customer");

            var store = _storeContext.CurrentStore;
            languageId = EnsureLanguageIsActive(languageId, store.Id);

            var messageTemplate = GetActiveMessageTemplate("NewVATSubmitted.StoreOwnerNotification", store.Id);
            if (messageTemplate == null)
                return 0;

            //email account
            var emailAccount = GetEmailAccountOfMessageTemplate(messageTemplate, languageId);

            //tokens
            var tokens = new List<Token>();
            _messageTokenProvider.AddStoreTokens(tokens, store, emailAccount);
            _messageTokenProvider.AddCustomerTokens(tokens, customer);
            tokens.Add(new Token("VatValidationResult.Name", vatName));
            tokens.Add(new Token("VatValidationResult.Address", vatAddress));

            //event notification
            _eventPublisher.MessageTokensAdded(messageTemplate, tokens);

            var toEmail = emailAccount.Email;
            var toName = emailAccount.DisplayName;
            return SendNotification(messageTemplate, emailAccount,
                languageId, tokens,
                toEmail, toName);
        }

        /// <summary>
        /// Sends a blog comment notification message to a store owner
        /// </summary>
        /// <param name="blogComment">Blog comment</param>
        /// <param name="languageId">Message language identifier</param>
        /// <returns>Queued email identifier</returns>
        public virtual int SendBlogCommentNotificationMessage(BlogComment blogComment, int languageId)
        {
            if (blogComment == null)
                throw new ArgumentNullException("blogComment");

            var store = _storeContext.CurrentStore;
            languageId = EnsureLanguageIsActive(languageId, store.Id);

            var messageTemplate = GetActiveMessageTemplate("Blog.BlogComment", store.Id);
            if (messageTemplate == null)
                return 0;

            //email account
            var emailAccount = GetEmailAccountOfMessageTemplate(messageTemplate, languageId);

            //tokens
            var tokens = new List<Token>();
            _messageTokenProvider.AddStoreTokens(tokens, store, emailAccount);
            _messageTokenProvider.AddBlogCommentTokens(tokens, blogComment);
            _messageTokenProvider.AddCustomerTokens(tokens, blogComment.Customer);

            //event notification
            _eventPublisher.MessageTokensAdded(messageTemplate, tokens);

            var toEmail = emailAccount.Email;
            var toName = emailAccount.DisplayName;
            return SendNotification(messageTemplate, emailAccount,
                languageId, tokens,
                toEmail, toName);
        }

        /// <summary>
        /// Sends a news comment notification message to a store owner
        /// </summary>
        /// <param name="newsComment">News comment</param>
        /// <param name="languageId">Message language identifier</param>
        /// <returns>Queued email identifier</returns>
        public virtual int SendNewsCommentNotificationMessage(NewsComment newsComment, int languageId)
        {
            if (newsComment == null)
                throw new ArgumentNullException("newsComment");

            var store = _storeContext.CurrentStore;
            languageId = EnsureLanguageIsActive(languageId, store.Id);

            var messageTemplate = GetActiveMessageTemplate("News.NewsComment", store.Id);
            if (messageTemplate == null)
                return 0;

            //email account
            var emailAccount = GetEmailAccountOfMessageTemplate(messageTemplate, languageId);

            //tokens
            var tokens = new List<Token>();
            _messageTokenProvider.AddStoreTokens(tokens, store, emailAccount);
            _messageTokenProvider.AddNewsCommentTokens(tokens, newsComment);
            _messageTokenProvider.AddCustomerTokens(tokens, newsComment.Customer);

            //event notification
            _eventPublisher.MessageTokensAdded(messageTemplate, tokens);

            var toEmail = emailAccount.Email;
            var toName = emailAccount.DisplayName;
            return SendNotification(messageTemplate, emailAccount,
                languageId, tokens,
                toEmail, toName);
        }

        /// <summary>
        /// Sends a 'Back in stock' notification message to a customer
        /// </summary>
        /// <param name="subscription">Subscription</param>
        /// <param name="languageId">Message language identifier</param>
        /// <returns>Queued email identifier</returns>
        public virtual int SendBackInStockNotification(BackInStockSubscription subscription, int languageId)
        {
            if (subscription == null)
                throw new ArgumentNullException("subscription");

            var store = _storeService.GetStoreById(subscription.StoreId) ?? _storeContext.CurrentStore;
            languageId = EnsureLanguageIsActive(languageId, store.Id);

            var messageTemplate = GetActiveMessageTemplate("Customer.BackInStock", store.Id);
            if (messageTemplate == null)
                return 0;

            //email account
            var emailAccount = GetEmailAccountOfMessageTemplate(messageTemplate, languageId);

            //tokens
            var tokens = new List<Token>();
            _messageTokenProvider.AddStoreTokens(tokens, store, emailAccount);
            _messageTokenProvider.AddCustomerTokens(tokens, subscription.Customer);
            _messageTokenProvider.AddBackInStockTokens(tokens, subscription);

            //event notification
            _eventPublisher.MessageTokensAdded(messageTemplate, tokens);

            var customer = subscription.Customer;
            var toEmail = customer.Email;
            var toName = customer.GetFullName();
            return SendNotification(messageTemplate, emailAccount,
                languageId, tokens,
                toEmail, toName);
        }

        /// <summary>
        /// Sends a test email
        /// </summary>
        /// <param name="messageTemplateId">Message template identifier</param>
        /// <param name="sendToEmail">Send to email</param>
        /// <param name="tokens">Tokens</param>
        /// <param name="languageId">Message language identifier</param>
        /// <returns>Queued email identifier</returns>
        public virtual int SendTestEmail(int messageTemplateId, string sendToEmail, 
            List<Token> tokens, int languageId)
        {
            var messageTemplate = _messageTemplateService.GetMessageTemplateById(messageTemplateId);
            if (messageTemplate == null)
                throw new ArgumentException("Template cannot be loaded");

            //email account
            var emailAccount = GetEmailAccountOfMessageTemplate(messageTemplate, languageId);

            //event notification
            _eventPublisher.MessageTokensAdded(messageTemplate, tokens);

            return SendNotification(messageTemplate, emailAccount,
                languageId, tokens,
                sendToEmail, null);
        }

        #endregion

        #endregion
        
    }
}
