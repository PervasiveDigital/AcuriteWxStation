using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PervasiveDigital.Verdant.WxStationNode.AcuRite
{
    public class Report1 : IReport
    {
        public Report1(DateTime timeStamp, WindDirection dir)
        {
            this.TimeStamp = timeStamp;
            this.Direction = dir;
            this.WindHeading = GetHeading(dir);

        }

        public DateTime TimeStamp { get; private set; }
        public WindDirection Direction { get; private set; }
        public double WindHeading { get; private set; }

        private double GetHeading(WindDirection dir)
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

    public class Report1a : Report1
    {
        public Report1a(DateTime timestamp, WindDirection dir, double windSpeed) : base(timestamp, dir)
        {
            this.WindSpeed = windSpeed;
        }

        public double WindSpeed { get; private set; }
    }

    public class Report1b : Report1
    {
        public Report1b(DateTime timestamp, WindDirection dir, double temperature, int relHum) : base(timestamp, dir)
        {
            this.Temperature = temperature;
            this.RelativeHumidity = relHum;
        }
        
        public double Temperature { get; private set; }
        public int RelativeHumidity { get; private set; }
    }
}
