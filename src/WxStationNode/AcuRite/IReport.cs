using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.Advertisement;

namespace PervasiveDigital.Verdant.WxStationNode.AcuRite
{
    public interface IReport
    {
        string DeviceId { get; }
    }
}
