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
        public static IReport ParseReport(byte[] data)
        {
            if (data==null || data.Length<1)
                throw new ArgumentException("null or empty data packet");

            switch (data[0])
            {
                case 1:
                    return ParseReport1(data);
                case 2:
                    return ParseReport2(data);
                default:
                    throw new ArgumentException("invalid packet - bad report number");
            }
        }

        public static Report1 ParseReport1(byte[] data)
        {
            if (data.Length!=10)
                throw new InvalidDataException("invalid length for report 1 packet");

            Report1 result = null;
            var nybble3 = data[3] & 0x0f;
            if (nybble3 == 1)
            {
                // wind speed, wind dir, rain
                double windSpeed = ((data[4] & 0x1f) << 3) | (data[5] & 0x70 >> 4);
                var windDir = data[5] & 0x0f;
                int rainCount = data[7] & 0x7f;
                result = new Report1a(DateTime.UtcNow, (WindDirection)windDir, windSpeed);
            }
            else if (nybble3 == 8)
            {
                // wind speed, temp, rel.hum
                var windDir = data[5] & 0x0f;
                double tempRaw = ((data[5] & 0x0f) << 7) | (data[6] & 0x7f);
                double temperature = (tempRaw - 1000.0) / 10.0;
                int relHum = data[7] & 0x7f;

                result = new Report1b(DateTime.UtcNow, (WindDirection)windDir, temperature, relHum);
            }
            else
                throw new InvalidDataException("invalid sub-record type");
            return result;
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
