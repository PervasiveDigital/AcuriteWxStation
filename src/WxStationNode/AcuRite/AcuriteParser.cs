using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PervasiveDigital.Verdant.WxStationNode.AcuRite
{
    public static class AcuriteParser
    {
        public static void ParseReport1(byte[] data, ref Report1 report)
        {
            if (data.Length!=10)
                throw new InvalidDataException("invalid length for report 1 packet");

            Report1 result = null;
            var nybble3 = data[3] & 0x0f;
            if (nybble3 == 1)
            {
                // wind speed, wind dir, rain
                report.WindSpeed = ((data[4] & 0x1f) << 3) | (data[5] & 0x70 >> 4);
                report.WindDirection = (WindDirection)(data[5] & 0x0f);
                report.RainCount = data[7] & 0x7f;
            }
            else if (nybble3 == 8)
            {
                // wind speed, temp, rel.hum
                report.WindDirection = (WindDirection)(data[5] & 0x0f);
                double tempRaw = ((data[5] & 0x0f) << 7) | (data[6] & 0x7f);
                report.Temperature = (((tempRaw - 400.0) / 10.0) - 32) * 5.0/9.0;
                report.RelativeHumidity = data[7] & 0x7f;
            }
            else
                throw new InvalidDataException("invalid sub-record type");
        }

        public static Report2 ParseReport2(byte[] data)
        {
            if (data.Length != 25)
                throw new InvalidDataException("invalid length for report 1 packet");

            Report2 result = null;

            return result;
        }
    }
}
