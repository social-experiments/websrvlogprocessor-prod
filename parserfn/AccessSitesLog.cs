using System;
using System.Collections.Generic;
using System.Text;

namespace ParserFn
{
    public class AccessedSitesLog
    {
        public string id { get; set; }
        public string PartitionKey { get; set; }
        public string Date { get; set; }
        public string Url { get; set; }
        public long Bandwidth { get; set; }
        public long MainModuleCount { get; set; }
        public long SubModuleCount { get; set; }
    }
}
