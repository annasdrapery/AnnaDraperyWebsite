using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Nop.Web.Models.Common
{
    public class CustomerCreditApplicationModel
    {
        public CustomerCreditApplicationModel()
        {
            SelectItems = new List<SelectListItem>()
            {
                new SelectListItem(){Text="Sole Proprietorship", Value="SPS"},
                new SelectListItem(){Text="Partnership", Value="PNS"},
                new SelectListItem(){Text="Corporation", Value="COR"},
                new SelectListItem(){Text="LLC", Value="LLC"}
            };
        }
        [Required]
        public string CompanyName { get; set; }
        [Required]
        public string Address { get; set; }
        [Required]
        public string City { get; set; }
        [Required]
        public string State { get; set; }
        [Required]
        public string ZipCode { get; set; }
        public bool SoleProprietorship { get; set; }
        public bool Partnership { get; set; }
        public bool Corporation { get; set; }
        public bool LLC { get; set; }
        [Required]
        public string StateTaxID { get; set; }
        [Required]
        public string Phone { get; set; }
        [Required]
        public string Cell { get; set; }
        public string Fax { get; set; }
        [Required]
        public string Email { get; set; }
        public string Company1 { get; set; }
        public string Address1 { get; set; }
        public string Phone1 { get; set; }
        public string Fax1 { get; set; }
        public string Company2 { get; set; }
        public string Address2 { get; set; }
        public string Phone2 { get; set; }
        public string Fax2 { get; set; }

        public List<SelectListItem> SelectItems { get; set; }
        public string SelectedText { get; set; }
        public bool DisplayCaptcha { get; set; }
    }
}