using System.Reflection.Metadata.Ecma335;

namespace BozjaBuddy.Data.Alarm
{
    public class MsgAlarm
    {
        public string _msg = "";
        public bool mIsDupable = true;

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
        public override bool Equals(object? obj)
        {
            if (obj is not null && obj!.GetType() == typeof(MsgAlarm))
            {
                return CompareMsg((obj as MsgAlarm)!);
            }
            return base.Equals(obj);
        }
        public override int GetHashCode()
        {
            return this._msg.GetHashCode();
        }
    }
}
