using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace Nop.Web.Models.Common
{
    public class SaleTaxCertificateModel
    {
        [Required]
        public string Purchaser1 { get; set; }
        [Required]
        public string Phone1 { get; set; }
        [Required]
        public string Address1 { get; set; }
        [Required]
        public string CSZ { get; set; }
        [Required]
        [StringLength(11, MinimumLength = 11)]
        public string Tax { get; set; }
        [Required]
        public string RFC { get; set; }
        [Required]
        public string Seller1 { get; set; }
        [Required]
        public string Street1 { get; set; }
        [Required]
        public string CSZ1 { get; set; }
        public string DescItem1 { get; set; }
        public string DescType1 { get; set; }
        [Required]
        public string Purchaser2 { get; set; }
        [Required]
        public string Address2 { get; set; }
        [Required]
        public string Phone2 { get; set; }
        [Required]
        public string CSZ2 { get; set; }
        [Required]
        public string Seller2 { get; set; }
        [Required]
        public string Street2 { get; set; }
        [Required]
        public string CSZ3 { get; set; }
        public string DescItem2 { get; set; }
        public string Reason { get; set; }
        public bool DisplayCaptcha { get; set; }
    }
}