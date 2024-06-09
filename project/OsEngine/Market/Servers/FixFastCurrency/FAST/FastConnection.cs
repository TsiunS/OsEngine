using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OsEngine.Market.Servers.FixFastCurrency.FAST
{
    public class FastConnection
    {
       public string FeedType { get; set; }
       public string FeedID { get; set; }
       public string MulticastIP { get; set; }
       public string SrsIP { get; set; }
       public int Port { get; set; }

    }
}
