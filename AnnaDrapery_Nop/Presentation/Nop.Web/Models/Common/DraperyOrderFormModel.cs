using System;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;
using FluentValidation.Attributes;
using Nop.Web.Framework;
using Nop.Web.Framework.Mvc;
using Nop.Web.Validators.Common;

namespace Nop.Web.Models.Common
{
    public partial class DraperyOrderFormModel : BaseNopModel
    {
        [Required]
        public string Designer { get; set; }
        [Required]
        public string SideMark { get; set; }
        [Required]
        public string Phone { get; set; }
        public string OrderDate { get; set; }
        public string DueDate { get; set; }
        public string Note { get; set; }
        [Required]
        [AllowHtml]
        public string OrderDetailJsonStr { get; set; }

        public bool DisplayCaptcha { get; set; }
    }
}