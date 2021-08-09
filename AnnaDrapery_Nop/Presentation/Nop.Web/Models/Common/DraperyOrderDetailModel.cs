using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Nop.Web.Models.Common
{
    public class DraperyOrderDetailModel
    {
        public string Line { get; set; }
        public string RoomLocation { get; set; }
        public int Qty { get; set; }
        public string Style { get; set; }
        public string Type { get; set; }
        public string BRBR { get; set; }
        public string Return { get; set; }
        public string Overlap { get; set; }
        public string Hoodset { get; set; }
        public string Fullness { get; set; }
        public string FinishedLength { get; set; }
        public string FabricNameColor { get; set; }
        public string LiningNameColor { get; set; }
        public string TopHeader { get; set; }
        public string TopPocket { get; set; }
        public string BottomHeader { get; set; }
        public string BottomPocket { get; set; }
        public string CoverredArea { get; set; }
        public string NoOfButtons { get; set; }
        public string NoOfWidth { get; set; }
        public string OverlapButtonLocation { get; set; }
        public string FabricWidth { get; set; }
        public string FinishedWidth { get; set; }
        public string LeftNoWidth { get; set; }
        public string RightNoWidth { get; set; }
        public string LeftSpace { get; set; }
        public string LeftNoOfPleats { get; set; }
        public string LeftPleat { get; set; }
        public string RightSpace { get; set; }
        public string RightNoOfPleats { get; set; }
        public string RightPleat { get; set; }
        public string YardLeft { get; set; }
        public string YardRight { get; set; }

    }
}