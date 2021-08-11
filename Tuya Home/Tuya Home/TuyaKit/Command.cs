using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tuya_Home.Kit
{
    public enum Command
    {
        UDP = 0x00,
        AP_CONFIG = 0x01,
        ACTIVE = 0x02,
        BIND = 0x03,
        RENAME_GW = 0x04,
        RENAME_DEVICE = 0x05,
        UNBIND = 0x06,
        CONTROL = 0x07,
        STATUS = 0x08,
        HEART_BEAT = 0x09,
        DP_QUERY = 0x0a,
        QUERY_WIFI = 0x0b,
        TOKEN_BIND = 0x0c,
        CONTROL_NEW = 0x0d,
        ENABLE_WIFI = 0x0e,
        DP_QUERY_NEW = 0x10,
        SCENE_EXECUTE = 0x11,
        DP_REFRESH = 0x12,
        UDP_NEW = 0x13,
        AP_CONFIG_NEW = 0x14,
        LAN_GW_ACTIVE = 0xF0,
        LAN_SUB_DEV_REQUEST = 0xF1,
        LAN_DELETE_SUB_DEV = 0xF2,
        LAN_REPORT_SUB_DEV = 0xF3,
        LAN_SCENE = 0xF4,
        LAN_PUBLISH_CLOUD_CONFIG = 0xF5,
        LAN_PUBLISH_APP_CONFIG = 0xF6,
        LAN_EXPORT_APP_CONFIG = 0xF7,
        LAN_PUBLISH_SCENE_PANEL = 0xF8,
        LAN_REMOVE_GW = 0xF9,
        LAN_CHECK_GW_UPDATE = 0xFA,
        LAN_GW_UPDATE = 0xFB,
        LAN_SET_GW_CHANNEL = 0xFC
    }
}
