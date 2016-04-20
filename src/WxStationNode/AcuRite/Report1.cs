using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace PervasiveDigital.Verdant.WxStationNode.AcuRite
{
    public class Report1 : IReport
    {
        public Report1()
        {
            this.DeviceId = AzureIoT.DeviceId;
            this.TimeStamp = DateTime.UtcNow;
        }

        [JsonProperty("deviceId")]
        public string DeviceId { get; private set; }

        [JsonProperty("time")]
        public DateTime TimeStamp { get; private set; }

        private WindDirection _dir;

        [JsonProperty("windDir")]
        public WindDirection WindDirection
        {
            get { return _dir; }
            set
            {
                _dir = value;
                this.WindHeading = GetHeading(value);
            }
        }

        [JsonProperty("windHdg")]
        public double WindHeading { get; private set; }

        [JsonProperty("windSpeed")]
        public double WindSpeed { get; set; }

        [JsonProperty("temperature")]
        public double Temperature { get; set; }

        [JsonProperty("relHum")]
        public int RelativeHumidity { get; set; }

        [JsonProperty("rainCount")]
        public int RainCount { get; set; }

        private static double GetHeading(WindDirection dir)
        {
            switch (dir)
            {
                case WindDirection.North:
                    return 0.0;
                case WindDirection.NorthNorthEast:
                    return 1*22.5;
                case WindDirection.NorthEast:
                    return 2 * 22.5;
                case WindDirection.EastNorthEast:
                    return 3 * 22.5;
                case WindDirection.East:
                    return 4 * 22.5;
                case WindDirection.EastSouthEast:
                    return 5 * 22.5;
                case WindDirection.SouthEast:
                    return 6 * 22.5;
                case WindDirection.SouthSouthEast:
                    return 7 * 22.5;
                case WindDirection.South:
                    return 8 * 22.5;
                case WindDirection.SouthSouthWest:
                    return 9 * 22.5;
                case WindDirection.SouthWest:
                    return 10 * 22.5;
                case WindDirection.WestSouthWest:
                    return 11 * 22.5;
                case WindDirection.West:
                    return 12 * 22.5;
                case WindDirection.WestNorthWest:
                    return 13 * 22.5;
                case WindDirection.NorthWest:
                    return 14 * 22.5;
                case WindDirection.NorthNorthWest:
                    return 15 * 22.5;
            }
            return -1.0;
        }
    }
}
