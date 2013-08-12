using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TwitterLiveSearch.Models
{
    
    public class WebHook
    {
        public long time_ms { get; set; }
        public SubscribeEvent[] events { get; set; }
    }

    public class SubscribeEvent
    {
        public string name { get; set; }
        public string channel { get; set; }
    }


 
}