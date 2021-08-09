using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Nop.Web.Models.Common
{
    public class CompleteFormModel
    {
        public CompleteFormModel()
        {
            this.ResultText = "Thank You! Your submitation was sent to administrator";
        }
        public string Title { get; set; }
        public string DownloadUrl { get; set; }
        public string ResultText { get; set; }
    }
}