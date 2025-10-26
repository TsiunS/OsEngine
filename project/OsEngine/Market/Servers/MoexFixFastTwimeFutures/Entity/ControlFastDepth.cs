using System;
using System.Collections.Generic;

namespace OsEngine.Market.Servers.MoexFixFastTwimeFutures.Entity
{
    public class ControlFastDepth
    {
        public ControlFastDepth()
        {
            AsksF = new List<ControlDepthLevel>();
            BidsF = new List<ControlDepthLevel>();
        }

        public List<ControlDepthLevel> AsksF;

        public List<ControlDepthLevel> BidsF;
    }

    public class ControlDepthLevel
    {
        public double Ask;

        public double Bid;

        public double Price;

        public DateTime ImmutabilityTime { get; set; } = DateTime.Now;
    }
}