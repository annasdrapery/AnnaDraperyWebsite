using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web.Mvc;
using Nop.Core.Domain.Catalog;

namespace Nop.Services.Catalog
{
    public static class DraperyAttributeExtensions
    {
        public static IList<CustomItemType> PoleDiameterList()
        {
            return new List<CustomItemType>()
            {
                new CustomItemType(){Id=0.5, Text="1/2\"", CustomField1="½\""},
                new CustomItemType(){Id=0.625, Text="5/8\"", CustomField1="⅝\""},
                new CustomItemType(){Id=0.75, Text="3/4\"", CustomField1="¾\""},
                new CustomItemType(){Id=0.875, Text="7/8\"", CustomField1="⅞\""},
                new CustomItemType(){Id=1, Text="1\"", CustomField1="1\""},
                new CustomItemType(){Id=1.125, Text="1 1/8\"", CustomField1="1⅛\""},              
                new CustomItemType(){Id=1.1875, Text="1 3/16\"", CustomField1="1 3/16\""},
                new CustomItemType(){Id=1.25, Text="1 1/4\"", CustomField1="1¼\""},
                new CustomItemType(){Id=1.375, Text="1 3/8\"", CustomField1="1⅜\""},
                new CustomItemType(){Id=1.5, Text="1 1/2\"", CustomField1="1½\""},
                new CustomItemType(){Id=2, Text="2\"", CustomField1="2\""},
                new CustomItemType(){Id=2.25, Text="2 1/4\"", CustomField1="2¼\""},
                new CustomItemType(){Id=2.75, Text="2 3/4\"", CustomField1="2¾\""},
                new CustomItemType(){Id=3, Text="3\"", CustomField1="3\""}
            };
        }
        public static IList<CustomItemType> OuterDiameterList()
        {
            return new List<CustomItemType>()
            {
                new CustomItemType(){Id=1.5, Text="1 1/2\""},
                new CustomItemType(){Id=1.875, Text="1 7/8\""},
                new CustomItemType(){Id=2, Text="2\""},
                new CustomItemType(){Id=2.25, Text="2 1/4\""},
                new CustomItemType(){Id=2.375, Text="2 3/8\""},
                new CustomItemType(){Id=2.5, Text="2 1/2\""},
                new CustomItemType(){Id=2.625, Text="2 5/8\""},
                new CustomItemType(){Id=2.75, Text="2 3/4\""},
                new CustomItemType(){Id=3, Text="3\""},
                new CustomItemType(){Id=3.125, Text="3 1/8\""},
                new CustomItemType(){Id=3.25, Text="3 1/4\""},
                new CustomItemType(){Id=3.625, Text="3 5/8\""},
                new CustomItemType(){Id=4, Text="4\""},
                new CustomItemType(){Id=4.125, Text="4 1/8\""},
                new CustomItemType(){Id=4.25, Text="4 1/4\""},
                new CustomItemType(){Id=4.375, Text="4 3/8\""},
                new CustomItemType(){Id=4.5, Text="4 1/2\""},
                new CustomItemType(){Id=5, Text="5\""}
            };
        }
        public static IList<CustomItemType> InnerDiameterList()
        {
            return new List<CustomItemType>()
            {
                new CustomItemType(){Id=1.5, Text="1 1/2\""},
                new CustomItemType(){Id=1.875, Text="1 7/8\""},
                new CustomItemType(){Id=2, Text="2\""},
                new CustomItemType(){Id=2.25, Text="2 1/4\""},
                new CustomItemType(){Id=2.375, Text="2 3/8\""},
                new CustomItemType(){Id=2.5, Text="2 1/2\""},
                new CustomItemType(){Id=2.625, Text="2 5/8\""},
                new CustomItemType(){Id=2.75, Text="2 3/4\""},
                new CustomItemType(){Id=3, Text="3\""},
                new CustomItemType(){Id=3.125, Text="3 1/8\""},
                new CustomItemType(){Id=3.25, Text="3 1/4\""},
                new CustomItemType(){Id=3.625, Text="3 5/8\""},
                new CustomItemType(){Id=4, Text="4\""},
                new CustomItemType(){Id=4.125, Text="4 1/8\""},
                new CustomItemType(){Id=4.25, Text="4 1/4\""},
                new CustomItemType(){Id=4.375, Text="4 3/8\""},
                new CustomItemType(){Id=4.5, Text="4 1/2\""},
                new CustomItemType(){Id=5, Text="5\""}
            };
        }
        public static IList<CustomItemType> PoleLengthList()
        {
            return new List<CustomItemType>()
            {
                new CustomItemType(){Id=4, Text="4 Feet"},
                new CustomItemType(){Id=5, Text="5 Feet"},
                new CustomItemType(){Id=6, Text="6 Feet"},
                new CustomItemType(){Id=7, Text="7 Feet"},
                new CustomItemType(){Id=8, Text="8 Feet"},
                new CustomItemType(){Id=10, Text="10 Feet"},
                new CustomItemType(){Id=12, Text="12 Feet"},
                new CustomItemType(){Id=15, Text="15 Feet"},
                new CustomItemType(){Id=16, Text="16 Feet"}
            };
        }

        public static IList<CustomItemType> AdjustableSizeList()
        {
            return new List<CustomItemType>()
            {
                new CustomItemType(){CustomField1 = "18-24", Text="18\" - 24\""},
                new CustomItemType(){CustomField1 = "18-28", Text="18\" - 28\""},
                new CustomItemType(){CustomField1 = "24-36", Text="24\" - 36\""},
                new CustomItemType(){CustomField1 = "28-48", Text="28\" - 48\""},
                new CustomItemType(){CustomField1 = "30-48", Text="30\" - 48\""},
                new CustomItemType(){CustomField1 = "31-48", Text="31\" - 48\""},
                new CustomItemType(){CustomField1 = "32-50", Text="32\" - 50\""},
                new CustomItemType(){CustomField1 = "36-60", Text="36\" - 60\""},
                new CustomItemType(){CustomField1 = "38-66", Text="38\" - 66\""},
                new CustomItemType(){CustomField1="48-66", Text="48\" - 66\""},
                new CustomItemType(){CustomField1="48-86", Text="48\" - 86\""},
                new CustomItemType(){CustomField1="50-66", Text="50\" - 65\""},
                new CustomItemType(){CustomField1 = "51-86", Text="51\" - 86\""},
                new CustomItemType(){CustomField1 = "58-65", Text="58\" - 65\""},
                new CustomItemType(){CustomField1="66-120", Text="66\" - 120\""},
                new CustomItemType(){CustomField1="70-120", Text="70\" - 120\""},
                new CustomItemType(){CustomField1="86-120", Text="86\" - 120\""},
                new CustomItemType(){CustomField1="86-150", Text="86\" - 150\""},
                new CustomItemType(){CustomField1="86-152", Text="86\" - 152\""},
                new CustomItemType(){CustomField1="100-180", Text="100\" - 180\""},
                new CustomItemType(){CustomField1="100-280", Text="100\" - 280\""},               
                new CustomItemType(){CustomField1="120-180", Text="120\" - 180\""},   
                new CustomItemType(){CustomField1="160-300", Text="160\" - 300\""}, 
                new CustomItemType(){CustomField1="180-270", Text="180\" - 270\""}
            };
        }
        

        public static IList<SelectListItem> AvailablePoleDiameter()
        {
            return PoleDiameterList().Select(x => new SelectListItem() { Value = x.Id.ToString("0.0000"), Text = x.Text }).ToList();
        }
        public static IList<SelectListItem> AvailableOuterDiameter()
        {
            return OuterDiameterList().Select(x => new SelectListItem() { Value = x.Id.ToString("0.0000"), Text = x.Text }).ToList();
        }
        public static IList<SelectListItem> AvailableInnerDiameter()
        {
            return InnerDiameterList().Select(x => new SelectListItem() { Value = x.Id.ToString("0.0000"), Text = x.Text }).ToList();
        }
        public static IList<SelectListItem> AvailablePoleLength()
        {
            return PoleLengthList().Select(x => new SelectListItem() { Value = x.Id.ToString("0.0000"), Text = x.Text }).ToList();
        }
        public static IList<SelectListItem> AvailableAdjustableSizeList()
        {
            return AdjustableSizeList().Select(x => new SelectListItem() { Value = x.CustomField1, Text = x.Text }).ToList();
        }
    }
}
