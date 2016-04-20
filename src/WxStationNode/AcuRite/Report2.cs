using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace PervasiveDigital.Verdant.WxStationNode.AcuRite
{
    public class Report2 : IReport
    {
        public Report2()
        {
            this.DeviceId = AzureIoT.DeviceId;
        }

        [JsonProperty("deviceId")]
        public string DeviceId { get; private set; }
    }
}
