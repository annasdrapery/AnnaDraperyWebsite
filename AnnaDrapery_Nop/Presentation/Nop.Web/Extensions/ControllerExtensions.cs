using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Nop.Core;
using Nop.Core.Caching;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Media;
using Nop.Services.Catalog;
using Nop.Services.Directory;
using Nop.Services.Localization;
using Nop.Services.Media;
using Nop.Services.Security;
using Nop.Services.Seo;
using Nop.Services.Tax;
using Nop.Web.Infrastructure.Cache;
using Nop.Web.Models.Catalog;
using Nop.Web.Models.Media;
using Nop.Web.Framework;

namespace Nop.Web.Extensions
{
    //here we have some methods shared between controllers
    public static class ControllerExtensions
    {
        public static IList<ProductSpecificationModel> PrepareProductSpecificationModel(this Controller controller,
            IWorkContext workContext,
            ISpecificationAttributeService specificationAttributeService,
            ICacheManager cacheManager,
            Product product)
        {
            if (product == null)
                throw new ArgumentNullException("product");

            string cacheKey = string.Format(ModelCacheEventConsumer.PRODUCT_SPECS_MODEL_KEY, product.Id, workContext.WorkingLanguage.Id);
            return cacheManager.Get(cacheKey, () =>
                specificationAttributeService.GetProductSpecificationAttributes(product.Id, 0, null, true)
                .Select(psa =>
                {
                    var m = new ProductSpecificationModel
                    {
                        SpecificationAttributeId = psa.SpecificationAttributeOption.SpecificationAttributeId,
                        SpecificationAttributeName = psa.SpecificationAttributeOption.SpecificationAttribute.GetLocalized(x => x.Name),
                    };

                    switch (psa.AttributeType)
                    {
                        case SpecificationAttributeType.Option:
                            m.ValueRaw = HttpUtility.HtmlEncode(psa.SpecificationAttributeOption.GetLocalized(x => x.Name));
                            break;
                        case SpecificationAttributeType.CustomText:
                            m.ValueRaw = HttpUtility.HtmlEncode(psa.CustomValue);
                            break;
                        case SpecificationAttributeType.CustomHtmlText:
                            m.ValueRaw = psa.CustomValue;
                            break;
                        case SpecificationAttributeType.Hyperlink:
                            m.ValueRaw = string.Format("<a href='{0}' target='_blank'>{0}</a>", psa.CustomValue);
                            break;
                        default:
                            break;
                    }
                    return m;
                }).ToList()
            );
        }

        public static IEnumerable<ProductOverviewModel> PrepareProductOverviewModels(this Controller controller,
            IWorkContext workContext,
            IStoreContext storeContext,
            ICategoryService categoryService,
            IProductService productService,
            ISpecificationAttributeService specificationAttributeService,
            IPriceCalculationService priceCalculationService,
            IPriceFormatter priceFormatter,
            IPermissionService permissionService,
            ILocalizationService localizationService,
            ITaxService taxService,
            ICurrencyService currencyService,
            IPictureService pictureService,
            IWebHelper webHelper,
            ICacheManager cacheManager,
            CatalogSettings catalogSettings,
            MediaSettings mediaSettings,
            IEnumerable<Product> products,
            bool preparePriceModel = true, bool preparePictureModel = true,
            int? productThumbPictureSize = null, bool prepareSpecificationAttributes = false,
            bool forceRedirectionAfterAddingToCart = false,bool getAllPictureModel = false, 
            IBrandService brandService = null, IProductAttributeService productAttributeService = null)
        {
            if (products == null)
                throw new ArgumentNullException("products");

            var models = new List<ProductOverviewModel>();
            foreach (var product in products)
            {
                var model = new ProductOverviewModel
                {
                    Id = product.Id,
                    Name = product.GetLocalized(x => x.Name),
                    ShortDescription = product.GetLocalized(x => x.ShortDescription),
                    FullDescription = product.GetLocalized(x => x.FullDescription),
                    SeName = product.GetSeName(),
                    MarkAsNew = product.MarkAsNew &&
                        (!product.MarkAsNewStartDateTimeUtc.HasValue || product.MarkAsNewStartDateTimeUtc.Value < DateTime.UtcNow) &&
                        (!product.MarkAsNewEndDateTimeUtc.HasValue || product.MarkAsNewEndDateTimeUtc.Value > DateTime.UtcNow),
                    Packaged = product.Packaged,
                    PoleDiameter = FractionConverter.Convert( product.PoleDiameter)+"\"",
                    NumberOfBrackets = product.NumberOfBrackets,
                    NumberOfPoles=product.NumberOfPoles,
                    ProductType = product.ProductType
                    
                };
                //pole style
                if (product.PoleStyleId>0)
                {
                    var poleStyle= PoleStyle.Bamboo.ToSelectList(false).ToList().FirstOrDefault(x => x.Value == product.PoleStyleId.ToString());
                    if (poleStyle != null)
                    {
                        model.PoleStyle = poleStyle.Text;
                    }
                }
                //price
                if (preparePriceModel)
                {
                    #region Prepare product price

                    var priceModel = new ProductOverviewModel.ProductPriceModel
                    {
                        ForceRedirectionAfterAddingToCart = forceRedirectionAfterAddingToCart
                    };

                    switch (product.ProductType)
                    {
                        case ProductType.GroupedProduct:
                            {
                                #region Grouped product

                                var associatedProducts = productService.GetAssociatedProducts(product.Id, storeContext.CurrentStore.Id);

                                switch (associatedProducts.Count)
                                {
                                    case 0:
                                        {
                                            //no associated products
                                            //priceModel.DisableBuyButton = true;
                                            //priceModel.DisableWishlistButton = true;
                                            //compare products
                                            priceModel.DisableAddToCompareListButton = !catalogSettings.CompareProductsEnabled;
                                            //priceModel.AvailableForPreOrder = false;
                                        }
                                        break;
                                    default:
                                        {
                                            //we have at least one associated product
                                            //priceModel.DisableBuyButton = true;
                                            //priceModel.DisableWishlistButton = true;
                                            //compare products
                                            priceModel.DisableAddToCompareListButton = !catalogSettings.CompareProductsEnabled;
                                            //priceModel.AvailableForPreOrder = false;

                                            if (permissionService.Authorize(StandardPermissionProvider.DisplayPrices))
                                            {
                                                //find a minimum possible price
                                                decimal? minPossiblePrice = null;
                                                Product minPriceProduct = null;
                                                foreach (var associatedProduct in associatedProducts)
                                                {
                                                    //calculate for the maximum quantity (in case if we have tier prices)
                                                    var tmpPrice = priceCalculationService.GetFinalPrice(associatedProduct,
                                                        workContext.CurrentCustomer, decimal.Zero, true, int.MaxValue);
                                                    if (!minPossiblePrice.HasValue || tmpPrice < minPossiblePrice.Value)
                                                    {
                                                        minPriceProduct = associatedProduct;
                                                        minPossiblePrice = tmpPrice;
                                                    }
                                                }
                                                if (minPriceProduct != null && !minPriceProduct.CustomerEntersPrice)
                                                {
                                                    if (minPriceProduct.CallForPrice)
                                                    {
                                                        priceModel.OldPrice = null;
                                                        priceModel.Price = localizationService.GetResource("Products.CallForPrice");
                                                    }
                                                    else if (minPossiblePrice.HasValue)
                                                    {
                                                        //calculate prices
                                                        decimal taxRate;
                                                        decimal finalPriceBase = taxService.GetProductPrice(minPriceProduct, minPossiblePrice.Value, out taxRate);
                                                        decimal finalPrice = currencyService.ConvertFromPrimaryStoreCurrency(finalPriceBase, workContext.WorkingCurrency);

                                                        priceModel.OldPrice = null;
                                                        priceModel.Price = String.Format(localizationService.GetResource("Products.PriceRangeFrom"), priceFormatter.FormatPrice(finalPrice));
                                                        priceModel.PriceValue = finalPrice;
                                                    }
                                                    else
                                                    {
                                                        //Actually it's not possible (we presume that minimalPrice always has a value)
                                                        //We never should get here
                                                        Debug.WriteLine("Cannot calculate minPrice for product #{0}", product.Id);
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                //hide prices
                                                priceModel.OldPrice = null;
                                                priceModel.Price = null;
                                            }
                                        }
                                        break;
                                }

                                #endregion
                            }
                            break;
                        case ProductType.SimpleProduct:
                        default:
                            {
                                #region Simple product

                                //add to cart button
                                priceModel.DisableBuyButton = product.DisableBuyButton ||
                                    !permissionService.Authorize(StandardPermissionProvider.EnableShoppingCart) ||
                                    !permissionService.Authorize(StandardPermissionProvider.DisplayPrices);

                                //add to wishlist button
                                priceModel.DisableWishlistButton = product.DisableWishlistButton ||
                                    !permissionService.Authorize(StandardPermissionProvider.EnableWishlist) ||
                                    !permissionService.Authorize(StandardPermissionProvider.DisplayPrices);
                                //compare products
                                priceModel.DisableAddToCompareListButton = !catalogSettings.CompareProductsEnabled;

                                //rental
                                priceModel.IsRental = product.IsRental;

                                //pre-order
                                if (product.AvailableForPreOrder)
                                {
                                    priceModel.AvailableForPreOrder = !product.PreOrderAvailabilityStartDateTimeUtc.HasValue ||
                                        product.PreOrderAvailabilityStartDateTimeUtc.Value >= DateTime.UtcNow;
                                    priceModel.PreOrderAvailabilityStartDateTimeUtc = product.PreOrderAvailabilityStartDateTimeUtc;
                                }

                                //prices
                                if (permissionService.Authorize(StandardPermissionProvider.DisplayPrices))
                                {
                                    if (!product.CustomerEntersPrice)
                                    {
                                        if (product.CallForPrice)
                                        {
                                            //call for price
                                            priceModel.OldPrice = null;
                                            priceModel.Price = localizationService.GetResource("Products.CallForPrice");
                                        }
                                        else
                                        {
                                            var defaultColorName = string.Empty;
                                            if (productAttributeService != null)
                                            {
                                                var colorAttr = productAttributeService.GetProductAttributeMappingsByProductId(product.Id).Where(x => x.ProductAttribute.Name.Trim().ToLower() == "color").FirstOrDefault();
                                                if (colorAttr != null)
                                                {
                                                    var defaultColor = colorAttr.ProductAttributeValues.Where(x => x.IsPreSelected).FirstOrDefault();
                                                    if (defaultColor != null)
                                                    {
                                                        defaultColorName = defaultColor.Name;
                                                    }
                                                    else
                                                    {
                                                        defaultColor = colorAttr.ProductAttributeValues.OrderBy(x => x.DisplayOrder).FirstOrDefault();
                                                        if (defaultColor != null)
                                                        {
                                                            defaultColorName = defaultColor.Name;
                                                        }
                                                    }
                                                }
                                            }
                                            //calculate for the maximum quantity (in case if we have tier prices)

                                            //add price for brackets of complete rod set
                                            var additionalCharge = decimal.Zero;
                                            if (product.ProductType == ProductType.CompleteRodSets)
                                            {
                                                
                                                var bracketAttr = product.ProductAttributeMappings.FirstOrDefault(x => x.ProductAttribute.Name.ToLower().Trim() == "brackets");
                                                if (bracketAttr != null)
                                                {
                                                    var firstBracket = bracketAttr.ProductAttributeValues.Where(x => x.IsPreSelected).FirstOrDefault();
                                                    if (firstBracket != null && firstBracket.ComponentProductId.HasValue)
                                                    {
                                                        var bracket = productService.GetProductById(firstBracket.ComponentProductId.Value);
                                                        if (bracket != null)
                                                        {
                                                            var bracketColorAttr = productAttributeService.GetProductAttributeMappingsByProductId(bracket.Id).Where(x => x.ProductAttribute.Name.Trim().ToLower() == "color").FirstOrDefault();
                                                            if (bracketColorAttr!=null)
                                                            {
                                                                var defaultBracketColor = bracketColorAttr.ProductAttributeValues.Where(x => x.Name.Trim().ToLower() == defaultColorName.Trim().ToLower()).FirstOrDefault();
                                                                if (defaultBracketColor != null)
                                                                {
                                                                    additionalCharge += firstBracket.Quantity * (bracket.Price + defaultBracketColor.PriceAdjustment);
                                                                }
                                                                else
                                                                {
                                                                    additionalCharge += firstBracket.Quantity * bracket.Price;
                                                                }
                                                            } 
                                                        }
                                                        if (product.ExpectedProfit != 0)
                                                        {
                                                            additionalCharge = Math.Round(additionalCharge * (1 + ((decimal)product.ExpectedProfit / 100)), 2);
                                                        }
                                                    }
                                                }
                                            }
                                            decimal minPossiblePrice = priceCalculationService.GetFinalPrice(product,
                                                workContext.CurrentCustomer, additionalCharge, true, int.MaxValue, product.MinRodWidth, defaultColorName);

                                            decimal taxRate;
                                            decimal oldPriceBase = taxService.GetProductPrice(product, product.OldPrice, out taxRate);
                                            decimal finalPriceBase = taxService.GetProductPrice(product, minPossiblePrice, out taxRate);

                                            decimal oldPrice = currencyService.ConvertFromPrimaryStoreCurrency(oldPriceBase, workContext.WorkingCurrency);
                                            decimal finalPrice = currencyService.ConvertFromPrimaryStoreCurrency(finalPriceBase, workContext.WorkingCurrency);

                                            //do we have tier prices configured?
                                            var tierPrices = new List<TierPrice>();
                                            if (product.HasTierPrices)
                                            {
                                                tierPrices.AddRange(product.TierPrices
                                                    .OrderBy(tp => tp.Quantity)
                                                    .ToList()
                                                    .FilterByStore(storeContext.CurrentStore.Id)
                                                    .FilterForCustomer(workContext.CurrentCustomer)
                                                    .RemoveDuplicatedQuantities());
                                            }
                                            //When there is just one tier (with  qty 1), 
                                            //there are no actual savings in the list.
                                            bool displayFromMessage = tierPrices.Count > 0 &&
                                                !(tierPrices.Count == 1 && tierPrices[0].Quantity <= 1);
                                            if (displayFromMessage)
                                            {
                                                priceModel.OldPrice = null;
                                                priceModel.Price = String.Format(localizationService.GetResource("Products.PriceRangeFrom"), priceFormatter.FormatPrice(finalPrice));
                                                priceModel.PriceValue = finalPrice;
                                            }
                                            else
                                            {
                                                if (finalPriceBase != oldPriceBase && oldPriceBase != decimal.Zero)
                                                {
                                                    priceModel.OldPrice = priceFormatter.FormatPrice(oldPrice);
                                                    priceModel.Price = priceFormatter.FormatPrice(finalPrice);
                                                    priceModel.PriceValue = finalPrice;
                                                }
                                                else
                                                {
                                                    priceModel.OldPrice = null;
                                                    priceModel.Price = priceFormatter.FormatPrice(finalPrice);
                                                    priceModel.PriceValue = finalPrice;
                                                }
                                            }
                                            if (product.IsRental)
                                            {
                                                //rental product
                                                priceModel.OldPrice = priceFormatter.FormatRentalProductPeriod(product, priceModel.OldPrice);
                                                priceModel.Price = priceFormatter.FormatRentalProductPeriod(product, priceModel.Price);
                                            }
                                            if (product.ProductType == ProductType.DecorativeTraverseRods ||
                                                product.ProductType == ProductType.HeavyDutyCurtainRods
                                                )
                                            {
                                                priceModel.Price += " for " + product.MinRodWidth + "\"";
                                            }

                                            //property for German market
                                            //we display tax/shipping info only with "shipping enabled" for this product
                                            //we also ensure this it's not free shipping
                                            priceModel.DisplayTaxShippingInfo = catalogSettings.DisplayTaxShippingInfoProductBoxes
                                                && product.IsShipEnabled &&
                                                !product.IsFreeShipping;
                                        }
                                    }
                                }
                                else
                                {
                                    //hide prices
                                    priceModel.OldPrice = null;
                                    priceModel.Price = null;
                                }

                                #endregion
                            }
                            break;
                    }

                    model.ProductPrice = priceModel;

                    #endregion
                }

                //picture
                if (preparePictureModel)
                {
                    #region Prepare product picture

                    //If a size has been set in the view, we use it in priority
                    int pictureSize = productThumbPictureSize.HasValue ? productThumbPictureSize.Value : mediaSettings.ProductThumbPictureSize;
                    //prepare picture model
                    var defaultProductPictureCacheKey = string.Format(ModelCacheEventConsumer.PRODUCT_DEFAULTPICTURE_MODEL_KEY, product.Id, pictureSize, true, workContext.WorkingLanguage.Id, webHelper.IsCurrentConnectionSecured(), storeContext.CurrentStore.Id);
                    model.DefaultPictureModel = cacheManager.Get(defaultProductPictureCacheKey, () =>
                    {
                        var picture = pictureService.GetPicturesByProductId(product.Id, 1).FirstOrDefault();
                        var pictureModel = new PictureModel
                        {
                            ImageUrl = pictureService.GetPictureUrl(picture, pictureSize),
                            FullSizeImageUrl = pictureService.GetPictureUrl(picture)
                        };
                        //"title" attribute
                        pictureModel.Title = (picture != null && !string.IsNullOrEmpty(picture.TitleAttribute)) ?
                            picture.TitleAttribute :
                            string.Format(localizationService.GetResource("Media.Product.ImageLinkTitleFormat"), model.Name);
                        //"alt" attribute
                        pictureModel.AlternateText = (picture != null && !string.IsNullOrEmpty(picture.AltAttribute)) ?
                            picture.AltAttribute :
                            string.Format(localizationService.GetResource("Media.Product.ImageAlternateTextFormat"), model.Name);
                        
                        return pictureModel;
                    });


                    #endregion
                }
                if (getAllPictureModel)
                {
                    //If a size has been set in the view, we use it in priority
                    int pictureSize = 40;
                    //prepare picture model
                    var listProductPictureCacheKey = string.Format(ModelCacheEventConsumer.PRODUCT_PICTURELIST_MODEL_KEY, product.Id, pictureSize, true, workContext.WorkingLanguage.Id, webHelper.IsCurrentConnectionSecured(), storeContext.CurrentStore.Id);
                    model.ListPictureModel = cacheManager.Get(listProductPictureCacheKey, () =>
                    {
                        var pictures = pictureService.GetPicturesByProductId(product.Id);
                        var result = new List<PictureModel>();
                        foreach (var picture in pictures)
                        {
                            var pictureModel = new PictureModel
                            {
                                ImageUrl = pictureService.GetPictureUrl(picture, pictureSize),
                                FullSizeImageUrl = pictureService.GetPictureUrl(picture)
                            };
                            //"title" attribute
                            pictureModel.Title = (picture != null && !string.IsNullOrEmpty(picture.TitleAttribute)) ?
                                picture.TitleAttribute :
                                string.Format(localizationService.GetResource("Media.Product.ImageLinkTitleFormat"), model.Name);
                            //"alt" attribute
                            pictureModel.AlternateText = (picture != null && !string.IsNullOrEmpty(picture.AltAttribute)) ?
                                picture.AltAttribute :
                                string.Format(localizationService.GetResource("Media.Product.ImageAlternateTextFormat"), model.Name);

                            result.Add(pictureModel);
                        }
                        return result;
                    });
                }

                //brand
                if (brandService != null)
                {
                    var _brand = brandService.GetBrandById(product.BrandId);
                    if (_brand!=null)
                    {
                        model.BrandName = _brand.Name;
                    }
                }
                
                if (productAttributeService !=null)
                {
                    //color
                    var colorAttributeMapping = productAttributeService.GetProductAttributeMappingsByProductId(product.Id).Where(x => x.ProductAttribute.Name.ToLower().Trim() == "color").FirstOrDefault();
                    if (colorAttributeMapping != null)
                    {
                        var values = productAttributeService.GetProductAttributeValues(colorAttributeMapping.Id);
                        model.ListColorPictureModel = values.Select(x =>
                            {
                                var pictureModel = new PictureModel() {
                                    ImageUrl = pictureService.GetPictureUrl(x.PictureId, 25)
                                };
                                pictureModel.Title = x.Name;
                                
                                return pictureModel;
                            }
                            ).ToList();
                    }
                }
                //specs
                if (prepareSpecificationAttributes)
                {
                    model.SpecificationAttributeModels = PrepareProductSpecificationModel(controller, workContext,
                         specificationAttributeService, cacheManager, product);
                }

                //reviews
                model.ReviewOverviewModel = new ProductReviewOverviewModel
                {
                    ProductId = product.Id,
                    RatingSum = product.ApprovedRatingSum,
                    TotalReviews = product.ApprovedTotalReviews,
                    AllowCustomerReviews = product.AllowCustomerReviews
                };

                models.Add(model);
            }
            return models;
        }
    }
}