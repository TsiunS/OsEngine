using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OsEngine.Market.Servers.FixFastCurrency.FAST
{
    public class OrderChange
    {
        public string UniqueName { get; set; }
        public int RptSeq { get; set; }
        public string MDEntryID { get; set; }
        public string Action { get; set; }
        public string OrderType { get; set; }
        public decimal Price { get; set; }
        public decimal Volume { get; set; }
        public DateTime DateTime { get; set; }
    }

  
}
