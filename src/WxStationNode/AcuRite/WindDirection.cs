using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PervasiveDigital.Verdant.WxStationNode.AcuRite
{
    public enum WindDirection
    {
        // "NW",  "WSW", "WNW", "W", "NNW", "SW",  "N",   "SSW", "ENE", "SE",  "E",   "ESE", "NE",  "SSE", "NNE", "S"
        NorthWest = 0,
        WestSouthWest = 1,
        WestNorthWest = 2,
        West = 3,
        NorthNorthWest = 4,
        SouthWest = 5,
        North = 6,
        SouthSouthWest = 7,
        EastNorthEast = 8,
        SouthEast = 9,
        East = 10,
        EastSouthEast = 11,
        NorthEast = 12,
        SouthSouthEast = 13,
        NorthNorthEast = 14,
        South = 15,
    }
}
