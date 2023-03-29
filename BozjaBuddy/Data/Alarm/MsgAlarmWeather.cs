using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BozjaBuddy.Data.Alarm
{
    public class MsgAlarmWeather : MsgAlarm
    {
        public MsgAlarmWeather(string pTerritoryId, int pWeatherId)
        {
            this._msg = $"{pTerritoryId}{pWeatherId}";
        }
    }
}
