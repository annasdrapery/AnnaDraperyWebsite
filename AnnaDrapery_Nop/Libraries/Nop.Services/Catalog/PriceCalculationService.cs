using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Nop.Core;
using Nop.Core.Caching;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Discounts;
using Nop.Core.Domain.Orders;
using Nop.Services.Catalog.Cache;
using Nop.Services.Customers;
using Nop.Services.Discounts;

namespace Nop.Services.Catalog
{
    /// <summary>
    /// Price calculation service
    /// </summary>
    public partial class PriceCalculationService : IPriceCalculationService
    {
        #region Fields

        private readonly IWorkContext _workContext;
        private readonly IStoreContext _storeContext;
        private readonly IDiscountService _discountService;
        private readonly ICategoryService _categoryService;
        private readonly IManufacturerService _manufacturerService;
        private readonly IProductAttributeParser _productAttributeParser;
        private readonly IProductService _productService;
        private readonly ICacheManager _cacheManager;
        private readonly ShoppingCartSettings _shoppingCartSettings;
        private readonly IProductAttributeService _productAttributeService;
        private readonly CatalogSettings _catalogSettings;

        #endregion

        #region Ctor

        public PriceCalculationService(IWorkContext workContext,
            IStoreContext storeContext,
            IDiscountService discountService, 
            ICategoryService categoryService,
            IManufacturerService manufacturerService,
            IProductAttributeParser productAttributeParser, 
            IProductService productService,
            ICacheManager cacheManager,
            ShoppingCartSettings shoppingCartSettings,
            IProductAttributeService productAttributeService,
            CatalogSettings catalogSettings)
        {
            this._workContext = workContext;
            this._storeContext = storeContext;
            this._discountService = discountService;
            this._categoryService = categoryService;
            this._manufacturerService = manufacturerService;
            this._productAttributeParser = productAttributeParser;
            this._productService = productService;
            this._cacheManager = cacheManager;
            this._shoppingCartSettings = shoppingCartSettings;
            this._catalogSettings = catalogSettings;
            this._productAttributeService = productAttributeService;
        }
        
        #endregion

        #region Nested classes

        [Serializable]
        protected class ProductPriceForCaching
        {
            public decimal Price { get; set; }
            public decimal AppliedDiscountAmount { get; set; }
            public int AppliedDiscountId { get; set; }
        }
        #endregion

        #region Utilities

        /// <summary>
        /// Gets allowed discounts applied to product
        /// </summary>
        /// <param name="product">Product</param>
        /// <param name="customer">Customer</param>
        /// <returns>Discounts</returns>
        protected virtual IList<Discount> GetAllowedDiscountsAppliedToProduct(Product product, Customer customer)
        {
            var allowedDiscounts = new List<Discount>();
            if (_catalogSettings.IgnoreDiscounts)
                return allowedDiscounts;

            if (product.HasDiscountsApplied)
            {
                //we use this property ("HasDiscountsApplied") for performance optimziation to avoid unnecessary database calls
                foreach (var discount in product.AppliedDiscounts)
                {
                    if (_discountService.ValidateDiscount(discount, customer).IsValid &&
                        discount.DiscountType == DiscountType.AssignedToSkus &&
                        !allowedDiscounts.ContainsDiscount(discount))
                        allowedDiscounts.Add(discount);
                }
            }

            return allowedDiscounts;
        }

        /// <summary>
        /// Gets allowed discounts applied to categories
        /// </summary>
        /// <param name="product">Product</param>
        /// <param name="customer">Customer</param>
        /// <returns>Discounts</returns>
        protected virtual IList<Discount> GetAllowedDiscountsAppliedToCategories(Product product, Customer customer)
        {
            var allowedDiscounts = new List<Discount>();
            if (_catalogSettings.IgnoreDiscounts)
                return allowedDiscounts;

            foreach (var discount in _discountService.GetAllDiscounts(DiscountType.AssignedToCategories))
            {
                //load identifier of categories with this discount applied to
                var cacheKey = string.Format(PriceCacheEventConsumer.DISCOUNT_CATEGORY_IDS_MODEL_KEY,
                    discount.Id,
                    string.Join(",", customer.GetCustomerRoleIds()),
                    _storeContext.CurrentStore.Id);
                var appliedToCategoryIds = _cacheManager.Get(cacheKey, () =>
                {
                    var categoryIds = new List<int>();
                    foreach (var category in discount.AppliedToCategories)
                    {
                        if (!categoryIds.Contains(category.Id))
                            categoryIds.Add(category.Id);
                        if (discount.AppliedToSubCategories)
                        {
                            //include subcategories
                            foreach (var childCategoryId in _categoryService
                                .GetAllCategoriesByParentCategoryId(category.Id, false, true)
                                .Select(x => x.Id))
                            {
                                if (!categoryIds.Contains(childCategoryId))
                                    categoryIds.Add(childCategoryId);
                            }
                        }
                    }
                    return categoryIds;
                });

                //compare with categories of this product
                if (appliedToCategoryIds.Any())
                {
                    //load identifier of categories with this discount applied to
                    var cacheKey2 = string.Format(PriceCacheEventConsumer.DISCOUNT_PRODUCT_CATEGORY_IDS_MODEL_KEY,
                        product.Id,
                        string.Join(",", customer.GetCustomerRoleIds()),
                        _storeContext.CurrentStore.Id);
                    var categoryIds = _cacheManager.Get(cacheKey2, () =>
                        _categoryService
                        .GetProductCategoriesByProductId(product.Id)
                        .Select(x => x.CategoryId)
                        .ToList());
                    foreach (var id in categoryIds)
                    {
                        if (appliedToCategoryIds.Contains(id))
                        {
                            if (_discountService.ValidateDiscount(discount, customer).IsValid &&
                                discount.DiscountType == DiscountType.AssignedToCategories &&
                                !allowedDiscounts.ContainsDiscount(discount))
                                allowedDiscounts.Add(discount);
                        }
                    }
                }
            }

            return allowedDiscounts;
        }

        /// <summary>
        /// Gets allowed discounts applied to manufacturers
        /// </summary>
        /// <param name="product">Product</param>
        /// <param name="customer">Customer</param>
        /// <returns>Discounts</returns>
        protected virtual IList<Discount> GetAllowedDiscountsAppliedToManufacturers(Product product, Customer customer)
        {
            var allowedDiscounts = new List<Discount>();
            if (_catalogSettings.IgnoreDiscounts)
                return allowedDiscounts;

            foreach (var discount in _discountService.GetAllDiscounts(DiscountType.AssignedToManufacturers))
            {
                //load identifier of categories with this discount applied to
                var cacheKey = string.Format(PriceCacheEventConsumer.DISCOUNT_MANUFACTURER_IDS_MODEL_KEY,
                    discount.Id,
                    string.Join(",", customer.GetCustomerRoleIds()),
                    _storeContext.CurrentStore.Id);
                var appliedToManufacturerIds = _cacheManager.Get(cacheKey,
                    () => discount.AppliedToManufacturers.Select(x => x.Id).ToList());

                //compare with manufacturers of this product
                if (appliedToManufacturerIds.Any())
                {
                    //load identifier of categories with this discount applied to
                    var cacheKey2 = string.Format(PriceCacheEventConsumer.DISCOUNT_PRODUCT_MANUFACTURER_IDS_MODEL_KEY,
                        product.Id,
                        string.Join(",", customer.GetCustomerRoleIds()),
                        _storeContext.CurrentStore.Id);
                    var manufacturerIds = _cacheManager.Get(cacheKey2, () =>
                        _manufacturerService
                        .GetProductManufacturersByProductId(product.Id)
                        .Select(x => x.ManufacturerId)
                        .ToList());
                    foreach (var id in manufacturerIds)
                    {
                        if (appliedToManufacturerIds.Contains(id))
                        {
                            if (_discountService.ValidateDiscount(discount, customer).IsValid &&
                                discount.DiscountType == DiscountType.AssignedToManufacturers &&
                                !allowedDiscounts.ContainsDiscount(discount))
                                allowedDiscounts.Add(discount);
                        }
                    }
                }
            }

            return allowedDiscounts;
        }

        /// <summary>
        /// Gets allowed discounts
        /// </summary>
        /// <param name="product">Product</param>
        /// <param name="customer">Customer</param>
        /// <returns>Discounts</returns>
        protected virtual IList<Discount> GetAllowedDiscounts(Product product, Customer customer)
        {
            var allowedDiscounts = new List<Discount>();
            if (_catalogSettings.IgnoreDiscounts)
                return allowedDiscounts;

            //discounts applied to products
            foreach (var discount in GetAllowedDiscountsAppliedToProduct(product, customer))
                if (!allowedDiscounts.ContainsDiscount(discount))
                    allowedDiscounts.Add(discount);

            //discounts applied to categories
            foreach (var discount in GetAllowedDiscountsAppliedToCategories(product, customer))
                if (!allowedDiscounts.ContainsDiscount(discount))
                    allowedDiscounts.Add(discount);

            //discounts applied to manufacturers
            foreach (var discount in GetAllowedDiscountsAppliedToManufacturers(product, customer))
                if (!allowedDiscounts.ContainsDiscount(discount))
                    allowedDiscounts.Add(discount);

            return allowedDiscounts;
        }
        
        /// <summary>
        /// Gets a tier price
        /// </summary>
        /// <param name="product">Product</param>
        /// <param name="customer">Customer</param>
        /// <param name="quantity">Quantity</param>
        /// <returns>Price</returns>
        protected virtual decimal? GetMinimumTierPrice(Product product, Customer customer, int quantity)
        {
            if (!product.HasTierPrices)
                return decimal.Zero;

            var tierPrices = product.TierPrices
                .OrderBy(tp => tp.Quantity)
                .ToList()
                .FilterByStore(_storeContext.CurrentStore.Id)
                .FilterForCustomer(customer)
                .RemoveDuplicatedQuantities();

            int previousQty = 1;
            decimal? previousPrice = null;
            foreach (var tierPrice in tierPrices)
            {
                //check quantity
                if (quantity < tierPrice.Quantity)
                    continue;
                if (tierPrice.Quantity < previousQty)
                    continue;

                //save new price
                previousPrice = tierPrice.Price;
                previousQty = tierPrice.Quantity;
            }
            
            return previousPrice;
        }

        /// <summary>
        /// Gets discount amount
        /// </summary>
        /// <param name="product">Product</param>
        /// <param name="customer">The customer</param>
        /// <param name="productPriceWithoutDiscount">Already calculated product price without discount</param>
        /// <param name="appliedDiscount">Applied discount</param>
        /// <returns>Discount amount</returns>
        protected virtual decimal GetDiscountAmount(Product product,
            Customer customer,
            decimal productPriceWithoutDiscount,
            out Discount appliedDiscount)
        {
            if (product == null)
                throw new ArgumentNullException("product");

            appliedDiscount = null;
            decimal appliedDiscountAmount = decimal.Zero;

            //we don't apply discounts to products with price entered by a customer
            if (product.CustomerEntersPrice)
                return appliedDiscountAmount;

            //discounts are disabled
            if (_catalogSettings.IgnoreDiscounts)
                return appliedDiscountAmount;

            var allowedDiscounts = GetAllowedDiscounts(product, customer);

            //no discounts
            if (allowedDiscounts.Count == 0)
                return appliedDiscountAmount;

            appliedDiscount = allowedDiscounts.GetPreferredDiscount(productPriceWithoutDiscount);

            if (appliedDiscount != null)
                appliedDiscountAmount = appliedDiscount.GetDiscountAmount(productPriceWithoutDiscount);

            return appliedDiscountAmount;
        }


        #endregion

        #region Methods




        /// <summary>
        /// Gets the final price
        /// </summary>
        /// <param name="product">Product</param>
        /// <param name="customer">The customer</param>
        /// <param name="additionalCharge">Additional charge</param>
        /// <param name="includeDiscounts">A value indicating whether include discounts or not for final price computation</param>
        /// <param name="quantity">Shopping cart item quantity</param>
        /// <returns>Final price</returns>
        public virtual decimal GetFinalPrice(Product product,
            Customer customer,
            decimal additionalCharge = decimal.Zero,
            bool includeDiscounts = true,
            int quantity = 1, decimal selectedRodWidth = 0, string selectedColorName = null)
        {
            decimal discountAmount;
            Discount appliedDiscount;
            return GetFinalPrice(product, customer, additionalCharge, includeDiscounts,
                quantity, out discountAmount, out appliedDiscount, selectedRodWidth,selectedColorName);
        }
        /// <summary>
        /// Gets the final price
        /// </summary>
        /// <param name="product">Product</param>
        /// <param name="customer">The customer</param>
        /// <param name="additionalCharge">Additional charge</param>
        /// <param name="includeDiscounts">A value indicating whether include discounts or not for final price computation</param>
        /// <param name="quantity">Shopping cart item quantity</param>
        /// <param name="discountAmount">Applied discount amount</param>
        /// <param name="appliedDiscount">Applied discount</param>
        /// <returns>Final price</returns>
        public virtual decimal GetFinalPrice(Product product, 
            Customer customer,
            decimal additionalCharge, 
            bool includeDiscounts,
            int quantity,
            out decimal discountAmount,
            out Discount appliedDiscount,
            decimal selectedRodWidth = 0, string selectedColorName = null)
        {
            return GetFinalPrice(product, customer,
                additionalCharge, includeDiscounts, quantity,
                null, null,
                out discountAmount, out appliedDiscount,selectedRodWidth,selectedColorName);
        }
        /// <summary>
        /// Gets the final price
        /// </summary>
        /// <param name="product">Product</param>
        /// <param name="customer">The customer</param>
        /// <param name="additionalCharge">Additional charge</param>
        /// <param name="includeDiscounts">A value indicating whether include discounts or not for final price computation</param>
        /// <param name="quantity">Shopping cart item quantity</param>
        /// <param name="rentalStartDate">Rental period start date (for rental products)</param>
        /// <param name="rentalEndDate">Rental period end date (for rental products)</param>
        /// <param name="discountAmount">Applied discount amount</param>
        /// <param name="appliedDiscount">Applied discount</param>
        /// <returns>Final price</returns>
        public virtual decimal GetFinalPrice(Product product, 
            Customer customer,
            decimal additionalCharge, 
            bool includeDiscounts,
            int quantity,
            DateTime? rentalStartDate,
            DateTime? rentalEndDate,
            out decimal discountAmount,
            out Discount appliedDiscount,
            decimal selectedRodWidth = 0,
            string selectedColorName = null)
        {
            if (product == null)
                throw new ArgumentNullException("product");

            discountAmount = decimal.Zero;
            appliedDiscount = null;

            var cacheKey = string.Format(PriceCacheEventConsumer.PRODUCT_PRICE_MODEL_KEY,
                product.Id, 
                additionalCharge.ToString(CultureInfo.InvariantCulture),
                includeDiscounts, 
                quantity,
                string.Join(",", customer.GetCustomerRoleIds()),
                _storeContext.CurrentStore.Id);
            var cacheTime = _catalogSettings.CacheProductPrices ? 60 : 0;
            //we do not cache price for rental products
            //otherwise, it can cause memory leaks (to store all possible date period combinations)
            if (product.IsRental)
                cacheTime = 0;
            var cachedPrice = _cacheManager.Get(cacheKey, cacheTime, () =>
            {
                var result = new ProductPriceForCaching();

                //initial price
                decimal price = product.Price;
                //get price by rod width
                if (product.ProductType == ProductType.HeavyDutyCurtainRods || product.ProductType == ProductType.DecorativeTraverseRods)
                {
                    if (selectedRodWidth > (product.WidthOrUnder * 12))
                    {
                        price += Math.Ceiling((selectedRodWidth - (product.WidthOrUnder * 12)) / 12) * product.PriceEachAdditionalFoot;
                    }
                }
                else if (product.ProductType == ProductType.CompleteRodSets)
                {
                    // price = finial + pole
                    price = 0;
                    if (product.FinialProductId.HasValue)
                    {
                        var finial = _productService.GetProductById(product.FinialProductId.Value);
                        if (finial != null)
                        {
                            var finialColorAttr = _productAttributeService.GetProductAttributeMappingsByProductId(finial.Id).Where(x => x.ProductAttribute.Name.Trim().ToLower() == "color").FirstOrDefault();
                            if (finialColorAttr != null && selectedColorName != null)
                            {
                                var defaultFinialColor = finialColorAttr.ProductAttributeValues.Where(x => x.Name.Trim().ToLower() == selectedColorName.Trim().ToLower()).FirstOrDefault();
                                if (defaultFinialColor != null )
                                {
                                    price += product.NumberOfFinials * (finial.Price + defaultFinialColor.PriceAdjustment);
                                }
                                else
                                {
                                    price += product.NumberOfFinials * finial.Price;
                                }
                            }
                            else
                            {
                                price += product.NumberOfFinials * finial.Price;
                            }

                        }                        
                    }
                    if (product.PoleProductId.HasValue)
                    {
                        var pole = _productService.GetProductById(product.PoleProductId.Value);
                        if (pole != null)
                        {
                            var poleColorAttr = _productAttributeService.GetProductAttributeMappingsByProductId(pole.Id).Where(x => x.ProductAttribute.Name.Trim().ToLower() == "color").FirstOrDefault();
                            if (poleColorAttr != null && selectedColorName != null)
                            {
                                var defaultPoleColor = poleColorAttr.ProductAttributeValues.Where(x => x.Name.Trim().ToLower() == selectedColorName.Trim().ToLower()).FirstOrDefault();
                                if (defaultPoleColor != null)
                                {
                                    price += product.NumberOfPoles * (pole.Price + defaultPoleColor.PriceAdjustment);
                                }
                                else
                                {
                                    price += product.NumberOfPoles * pole.Price;
                                }
                            }
                            else
                            {
                                price += product.NumberOfPoles * pole.Price;
                            }
                            
                        }
                    }
                    
                }
                //get sale price
                if (product.ExpectedProfit != 0)
                {
                    price = Math.Round(price * (1 + ((decimal)product.ExpectedProfit / 100)), 2);
                }
                //special price
                var specialPrice = product.GetSpecialPrice();
                if (specialPrice.HasValue)
                    price = specialPrice.Value;

                //tier prices
                if (product.HasTierPrices)
                {
                    decimal? tierPrice = GetMinimumTierPrice(product, customer, quantity);
                    if (tierPrice.HasValue)
                        price = Math.Min(price, tierPrice.Value);
                }

                //additional charge
                price = price + additionalCharge;

                //rental products
                if (product.IsRental)
                    if (rentalStartDate.HasValue && rentalEndDate.HasValue)
                        price = price * product.GetRentalPeriods(rentalStartDate.Value, rentalEndDate.Value);

                if (includeDiscounts)
                {
                    //discount
                    Discount tmpAppliedDiscount;
                    decimal tmpDiscountAmount = GetDiscountAmount(product, customer, price, out tmpAppliedDiscount);
                    price = price - tmpDiscountAmount;

                    if (tmpAppliedDiscount != null)
                    {
                        result.AppliedDiscountId = tmpAppliedDiscount.Id;
                        result.AppliedDiscountAmount = tmpDiscountAmount;
                    }
                }

                if (price < decimal.Zero)
                    price = decimal.Zero;

                result.Price = price;
                return result;
            });

            if (includeDiscounts)
            {
                //Discount instance cannnot be cached between requests (when "catalogSettings.CacheProductPrices" is "true)
                //This is limitation of Entity Framework
                //That's why we load it here after working with cache
                appliedDiscount = _discountService.GetDiscountById(cachedPrice.AppliedDiscountId);
                if (appliedDiscount != null)
                {
                    discountAmount = cachedPrice.AppliedDiscountAmount;
                }
            }

            return cachedPrice.Price;
        }



        /// <summary>
        /// Gets the shopping cart unit price (one item)
        /// </summary>
        /// <param name="shoppingCartItem">The shopping cart item</param>
        /// <param name="includeDiscounts">A value indicating whether include discounts or not for price computation</param>
        /// <returns>Shopping cart unit price (one item)</returns>
        public virtual decimal GetUnitPrice(ShoppingCartItem shoppingCartItem,
            bool includeDiscounts = true)
        {
            decimal discountAmount;
            Discount appliedDiscount;
            return GetUnitPrice(shoppingCartItem, includeDiscounts,
                out discountAmount, out appliedDiscount);
        }
        /// <summary>
        /// Gets the shopping cart unit price (one item)
        /// </summary>
        /// <param name="shoppingCartItem">The shopping cart item</param>
        /// <param name="includeDiscounts">A value indicating whether include discounts or not for price computation</param>
        /// <param name="discountAmount">Applied discount amount</param>
        /// <param name="appliedDiscount">Applied discount</param>
        /// <returns>Shopping cart unit price (one item)</returns>
        public virtual decimal GetUnitPrice(ShoppingCartItem shoppingCartItem,
            bool includeDiscounts,
            out decimal discountAmount,
            out Discount appliedDiscount)
        {
            if (shoppingCartItem == null)
                throw new ArgumentNullException("shoppingCartItem");

            return GetUnitPrice(shoppingCartItem.Product,
                shoppingCartItem.Customer,
                shoppingCartItem.ShoppingCartType,
                shoppingCartItem.Quantity,
                shoppingCartItem.AttributesXml,
                shoppingCartItem.CustomerEnteredPrice,
                shoppingCartItem.RentalStartDateUtc,
                shoppingCartItem.RentalEndDateUtc,
                includeDiscounts,
                out discountAmount,
                out appliedDiscount, selectedRodWidth: (shoppingCartItem.SelectedRodWidth + shoppingCartItem.SelectedRodWidtheven));
        }
        /// <summary>
        /// Gets the shopping cart unit price (one item)
        /// </summary>
        /// <param name="product">Product</param>
        /// <param name="customer">Customer</param>
        /// <param name="shoppingCartType">Shopping cart type</param>
        /// <param name="quantity">Quantity</param>
        /// <param name="attributesXml">Product atrributes (XML format)</param>
        /// <param name="customerEnteredPrice">Customer entered price (if specified)</param>
        /// <param name="rentalStartDate">Rental start date (null for not rental products)</param>
        /// <param name="rentalEndDate">Rental end date (null for not rental products)</param>
        /// <param name="includeDiscounts">A value indicating whether include discounts or not for price computation</param>
        /// <param name="discountAmount">Applied discount amount</param>
        /// <param name="appliedDiscount">Applied discount</param>
        /// <returns>Shopping cart unit price (one item)</returns>
        public virtual decimal GetUnitPrice(Product product,
            Customer customer, 
            ShoppingCartType shoppingCartType,
            int quantity,
            string attributesXml,
            decimal customerEnteredPrice,
            DateTime? rentalStartDate, DateTime? rentalEndDate,
            bool includeDiscounts,
            out decimal discountAmount,
            out Discount appliedDiscount,
            decimal selectedRodWidth = 0)
        {
            if (product == null)
                throw new ArgumentNullException("product");

            if (customer == null)
                throw new ArgumentNullException("customer");

            discountAmount = decimal.Zero;
            appliedDiscount = null;

            decimal finalPrice;

            var combination = _productAttributeParser.FindProductAttributeCombination(product, attributesXml);
            if (combination != null && combination.OverriddenPrice.HasValue)
            {
                finalPrice = combination.OverriddenPrice.Value;
            }
            else
            {
                //summarize price of all attributes
                decimal attributesTotalPrice = decimal.Zero;
                var attributeValues = _productAttributeParser.ParseProductAttributeValues(attributesXml);
                //get selected color
                string selectedColorName = null;
                var colorAttr = attributeValues.Where(x => x.ProductAttributeMapping.ProductAttribute.Name.Trim().ToLower() == "color").FirstOrDefault();
                if (colorAttr != null)
                {
                    selectedColorName = colorAttr.Name;
                }
                if (attributeValues != null)
                {
                    
                    foreach (var attributeValue in attributeValues)
                    {
                        attributesTotalPrice += GetProductAttributeValuePriceAdjustment(attributeValue, selectedColorName, selectedRodWidth);
                    }
                }

                //get price of a product (with previously calculated price of all attributes)
                if (product.CustomerEntersPrice)
                {
                    finalPrice = customerEnteredPrice;
                }
                else
                {
                    int qty;
                    if (_shoppingCartSettings.GroupTierPricesForDistinctShoppingCartItems)
                    {
                        //the same products with distinct product attributes could be stored as distinct "ShoppingCartItem" records
                        //so let's find how many of the current products are in the cart
                        qty = customer.ShoppingCartItems
                            .Where(x => x.ProductId == product.Id)
                            .Where(x => x.ShoppingCartType == shoppingCartType)
                            .Sum(x => x.Quantity);
                        if (qty == 0)
                        {
                            qty = quantity;
                        }
                    }
                    else
                    {
                        qty = quantity;
                    }
                    finalPrice = GetFinalPrice(product,
                        customer,
                        attributesTotalPrice,
                        includeDiscounts,
                        qty,
                        product.IsRental ? rentalStartDate : null,
                        product.IsRental ? rentalEndDate : null,
                        out discountAmount, out appliedDiscount, selectedRodWidth);
                }
            }
            
            //rounding
            if (_shoppingCartSettings.RoundPricesDuringCalculation)
                finalPrice = RoundingHelper.RoundPrice(finalPrice);

            return finalPrice;
        }
        /// <summary>
        /// Gets the shopping cart item sub total
        /// </summary>
        /// <param name="shoppingCartItem">The shopping cart item</param>
        /// <param name="includeDiscounts">A value indicating whether include discounts or not for price computation</param>
        /// <returns>Shopping cart item sub total</returns>
        public virtual decimal GetSubTotal(ShoppingCartItem shoppingCartItem,
            bool includeDiscounts = true)
        {
            decimal discountAmount;
            Discount appliedDiscount;
            return GetSubTotal(shoppingCartItem, includeDiscounts, out discountAmount, out appliedDiscount);
        }
        /// <summary>
        /// Gets the shopping cart item sub total
        /// </summary>
        /// <param name="shoppingCartItem">The shopping cart item</param>
        /// <param name="includeDiscounts">A value indicating whether include discounts or not for price computation</param>
        /// <param name="discountAmount">Applied discount amount</param>
        /// <param name="appliedDiscount">Applied discount</param>
        /// <returns>Shopping cart item sub total</returns>
        public virtual decimal GetSubTotal(ShoppingCartItem shoppingCartItem,
            bool includeDiscounts,
            out decimal discountAmount,
            out Discount appliedDiscount)
        {
            if (shoppingCartItem == null)
                throw new ArgumentNullException("shoppingCartItem");

            decimal subTotal;

            //unit price
            var unitPrice = GetUnitPrice(shoppingCartItem, includeDiscounts,
                out discountAmount, out appliedDiscount);

            //discount
            if (appliedDiscount != null)
            {
                if (appliedDiscount.MaximumDiscountedQuantity.HasValue &&
                    shoppingCartItem.Quantity > appliedDiscount.MaximumDiscountedQuantity.Value)
                {
                    //we cannot apply discount for all shopping cart items
                    var discountedQuantity = appliedDiscount.MaximumDiscountedQuantity.Value;
                    var discountedSubTotal = unitPrice * discountedQuantity;
                    discountAmount = discountAmount * discountedQuantity;

                    var notDiscountedQuantity = shoppingCartItem.Quantity - discountedQuantity;
                    var notDiscountedUnitPrice = GetUnitPrice(shoppingCartItem, false);
                    var notDiscountedSubTotal = notDiscountedUnitPrice*notDiscountedQuantity;

                    subTotal = discountedSubTotal + notDiscountedSubTotal;
                }
                else
                {
                    //discount is applied to all items (quantity)
                    //calculate discount amount for all items
                    discountAmount = discountAmount * shoppingCartItem.Quantity;

                    subTotal = unitPrice * shoppingCartItem.Quantity;
                }
            }
            else
            {
                subTotal = unitPrice * shoppingCartItem.Quantity;
            }
            return subTotal;
        }
        


        /// <summary>
        /// Gets the product cost (one item)
        /// </summary>
        /// <param name="product">Product</param>
        /// <param name="attributesXml">Shopping cart item attributes in XML</param>
        /// <returns>Product cost (one item)</returns>
        public virtual decimal GetProductCost(Product product, string attributesXml)
        {
            if (product == null)
                throw new ArgumentNullException("product");

            decimal cost = product.ProductCost;
            var attributeValues = _productAttributeParser.ParseProductAttributeValues(attributesXml);
            foreach (var attributeValue in attributeValues)
            {
                switch (attributeValue.AttributeValueType)
                {
                    case AttributeValueType.Simple:
                        {
                            //simple attribute
                            cost += attributeValue.Cost;
                        }
                        break;
                    case AttributeValueType.AssociatedToProduct:
                        {
                            //bundled product
                            var associatedProduct = _productService.GetProductById(attributeValue.AssociatedProductId);
                            if (associatedProduct != null)
                                cost += associatedProduct.ProductCost * attributeValue.Quantity;
                        }
                        break;
                    default:
                        break;
                }
            }

            return cost;
        }



        /// <summary>
        /// Get a price adjustment of a product attribute value
        /// </summary>
        /// <param name="value">Product attribute value</param>
        /// <returns>Price adjustment</returns>
        public virtual decimal GetProductAttributeValuePriceAdjustment(ProductAttributeValue value, string selectedColorName = null, decimal selectedRodWidth = 0)
        {
            if (value == null)
                throw new ArgumentNullException("value");

            var adjustment = decimal.Zero;
             var product = value.ProductAttributeMapping.Product;
            switch (value.AttributeValueType)
            {
                case AttributeValueType.Simple:
                    {
                        //simple attribute
                        adjustment = value.PriceAdjustment;

                        //get price of finial, pole follow color name in complete rod set
                       
                        if (product.ProductType == ProductType.CompleteRodSets)
                        {
                            if (value.ProductAttributeMapping.ProductAttribute.Name.Trim().ToLower() == "color")
                            {
                                adjustment = decimal.Zero;
                                if (product.FinialProductId.HasValue)
                                {
                                    var finial = _productService.GetProductById(product.FinialProductId.Value);
                                    if (finial != null)
                                    {
                                        var colorAttr = finial.ProductAttributeMappings.Where(x => x.ProductAttribute.Name.Trim().ToLower() == "color").FirstOrDefault();
                                        if (colorAttr != null)
                                        {
                                            var selectedColor = colorAttr.ProductAttributeValues.Where(x => x.Name.Trim().ToLower() == value.Name.Trim().ToLower()).FirstOrDefault();
                                            if (selectedColor != null)
                                            {
                                                adjustment += product.NumberOfFinials * Math.Round(selectedColor.PriceAdjustment * (1 + ((decimal)product.ExpectedProfit / 100)), 2);
                                            }
                                        }
                                    }
                                }
                                if (product.PoleProductId.HasValue)
                                {
                                    var pole = _productService.GetProductById(product.PoleProductId.Value);
                                    if (pole != null)
                                    {
                                        var colorAttr = pole.ProductAttributeMappings.Where(x => x.ProductAttribute.Name.Trim().ToLower() == "color").FirstOrDefault();
                                        if (colorAttr != null)
                                        {
                                            var selectedColor = colorAttr.ProductAttributeValues.Where(x => x.Name.Trim().ToLower() == value.Name.Trim().ToLower()).FirstOrDefault();
                                            if (selectedColor != null)
                                            {
                                                adjustment += Math.Round(product.NumberOfPoles * selectedColor.PriceAdjustment * (1 + ((decimal)product.ExpectedProfit / 100)), 2);
                                            }
                                        }
                                    }
                                }
                            }
                            else if ((value.ProductAttributeMapping.ProductAttribute.Name.Trim().ToLower() == "brackets"
                                || value.ProductAttributeMapping.ProductAttribute.Name.Trim().ToLower() == "ring quantity" )
                                && selectedColorName != null)
                            {
                                adjustment = decimal.Zero;
                                if (value.ComponentProductId.HasValue)
                                {
                                    var component = _productService.GetProductById(value.ComponentProductId.Value);
                                    if (component!=null)
                                    {
                                        var colorAttr = component.ProductAttributeMappings.Where(x => x.ProductAttribute.Name.Trim().ToLower() == "color").FirstOrDefault();
                                        if (colorAttr != null)
                                        {
                                            var selectedColor = colorAttr.ProductAttributeValues.Where(x => x.Name.Trim().ToLower() == selectedColorName.Trim().ToLower()).FirstOrDefault();
                                            if (selectedColor != null)
                                            {
                                                adjustment += Math.Round(value.Quantity * (component.Price + selectedColor.PriceAdjustment) * (1 + ((decimal)product.ExpectedProfit / 100)), 2);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        //calculate addtional price follow selected color
                        else if (product.ProductType == ProductType.DecorativeTraverseRods)
                        {
                            if (value.ProductAttributeMapping.ProductAttribute.Name.Trim().ToLower() == "color")
                            {
                                adjustment = decimal.Zero;
                                adjustment += Math.Ceiling(selectedRodWidth / 12) * value.PriceAdjustment;
                                
                            }
                            else if ((value.ProductAttributeMapping.ProductAttribute.Name.Trim().ToLower() == "finials"
                                || value.ProductAttributeMapping.ProductAttribute.Name.Trim().ToLower() == "brackets")
                                && value.ComponentProductId.HasValue
                                )
                            {
                                var component = _productService.GetProductById(value.ComponentProductId.Value);
                                if (component != null)
                                {
                                    adjustment = decimal.Zero;
                                    if (component.ProductType == ProductType.Finials)
                                    {
                                        var colorAttr = component.ProductAttributeMappings.Where(x => x.ProductAttribute.Name.Trim().ToLower() == "color").FirstOrDefault();
                                        if (colorAttr != null && selectedColorName!=null)
                                        {
                                            var selectedColor = colorAttr.ProductAttributeValues.Where(x => x.Name.Trim().ToLower() == selectedColorName.Trim().ToLower()).FirstOrDefault();
                                            if (selectedColor != null)
                                            {
                                                adjustment = value.Quantity *( component.Price + selectedColor.PriceAdjustment);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        adjustment = value.Quantity * component.Price;
                                    }

                                }
                            }
                        }
                        
                    }
                    break;
                case AttributeValueType.AssociatedToProduct:
                    {
                        //bundled product
                        var associatedProduct = _productService.GetProductById(value.AssociatedProductId);
                        if (associatedProduct != null)
                        {
                            adjustment = GetFinalPrice(associatedProduct, _workContext.CurrentCustomer, includeDiscounts: true) * value.Quantity;
                        }
                    }
                    break;
                default:
                    break;
            }
            if (product.ProductType == ProductType.DecorativeTraverseRods && 
                (value.ProductAttributeMapping.ProductAttribute.Name.Trim().ToLower() == "color"
                || value.ProductAttributeMapping.ProductAttribute.Name.Trim().ToLower() == "finials"
                || value.ProductAttributeMapping.ProductAttribute.Name.Trim().ToLower() == "brackets"
                ))
            {
                if (product.ExpectedProfit != 0)
                {
                    adjustment = Math.Round(adjustment * (1 + ((decimal)product.ExpectedProfit / 100)), 2);
                }
            }
            //add profit expect to adjustment 
            else if (value.ExpectedProfit != 0)
            {
                adjustment = Math.Round(adjustment * (1 + ((decimal)value.ExpectedProfit / 100)), 2);
            }
            return adjustment;
        }

        #endregion
    }
}
