using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BozjaBuddy.Data.Alarm
{
    internal class AlarmWeather : Alarm
    {
        protected new int mTrigger { get; set; }

        public AlarmWeather(int pTriggerWeatherId)
        {
            this.mId = _ID_COUNTER; Alarm._ID_COUNTER += 1;
            this.mName = $"Alarm #{this.mId}";
            this.mTrigger = pTriggerWeatherId;
        }
        public AlarmWeather(int pTriggerWeatherId, string pName)
        {
            this.mId = _ID_COUNTER; Alarm._ID_COUNTER += 1;
            this.mName = pName;
            this.mTrigger = pTriggerWeatherId;
        }

        public override bool CheckAlarm(Plugin pPlugin)
        {
            return true;
        }

            
    }
}
