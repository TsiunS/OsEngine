using OsEngine.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OsEngine.Market.Servers.FixFastCurrency.FAST
{
    public class WaitingTrade
    {
        public string UniqueName { get; set; }
        public int RptSeq {  get; set; }
        public Trade Trade;
    }
}
