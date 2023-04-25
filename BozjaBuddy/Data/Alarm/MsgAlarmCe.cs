using Dalamud.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BozjaBuddy.Data.Alarm
{
    internal class MsgAlarmCe : MsgAlarmFateCe
    {
        protected MsgAlarmCe() : base() { }
        public MsgAlarmCe(string pContent)
        {
            this.mIsDupable = false;
        }
        public override bool CompareMsg(MsgAlarm pMsgIn)
        {
            int tTemp;
            Int32.TryParse(this._msg, out tTemp);
            if (tTemp > 50) return false;
            //if (tTemp == AlarmFateCe.kTriggerInt_AcceptAllCe) return true;     // Accept all
            return base.CompareMsg(pMsgIn);
        }
    }
}
