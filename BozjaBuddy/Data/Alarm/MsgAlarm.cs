using System.Reflection.Metadata.Ecma335;

namespace BozjaBuddy.Data.Alarm
{
    public class MsgAlarm
    {
        public string _msg = "";

        protected MsgAlarm()
        { 
        }
        public MsgAlarm(string pContent)
        {
            this._msg = pContent;
        }
        public virtual bool CompareMsg(MsgAlarm pMsgIn)
        {
            if (pMsgIn._msg == this._msg) return true;
            return false;
        }
    }
}
