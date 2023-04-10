using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BozjaBuddy.Data.Alarm
{
    internal class MsgAlarmFateCe : MsgAlarm
    {
        protected MsgAlarmFateCe() : base() { }
        public MsgAlarmFateCe(string pContent) : base(pContent)
        {
            this.mIsDupable = false;
        }
    }
}
