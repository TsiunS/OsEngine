using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace OsEngine.Market.Servers.TInvestData.Entity
{
    public class ArchiveResponse
    {
        [JsonPropertyName("lastModified")]
        public string LastModified { get; set; }

        [JsonPropertyName("year")]
        public string Year { get; set; }

        [JsonPropertyName("entries")]
        public List<ArchiveEntry> Entries { get; set; }
    }

    public class ArchiveEntry
    {
        [JsonPropertyName("date")]
        public string Date { get; set; }

        [JsonPropertyName("uri")]
        public string Uri { get; set; }

        [JsonPropertyName("sizeTxt")]
        public string SizeTxt { get; set; }
    }
}
