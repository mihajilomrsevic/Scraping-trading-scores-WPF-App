using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Scraping_trading_scores_WPF_App
{
    public class DataScraper
    {
        [JsonProperty("tradedatetimegmt")]
        public DateTime tradedatetimegmt;
        [JsonProperty("open")]
        public double open;
        [JsonProperty("high")]
        public double high;
        [JsonProperty("low")]
        public double low;
        [JsonProperty("close")]
        public double close;
        [JsonProperty("volume")]
        public int volume;
    }
}
