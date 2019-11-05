using System;
using System.Collections.Generic;
using System.Text;

namespace ParserLib
{
    //AccessDataDetail Object to be serialized into JSON - One per day
    public class AccessDataDetail
    {
        public string DeviceId { get; set; }
        public string AccessDate { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public long Bandwidth { get; set; }
        public List<AccessData> AccessDetails { get; set; } = new List<AccessData>();
    }
}
