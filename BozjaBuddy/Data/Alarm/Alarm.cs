using System;

namespace BozjaBuddy.Data.Alarm
{
    public class Alarm
    {
        protected static int _ID_COUNTER = 0;
        public int mId { get; set; }
        public string mName { get; set; } = "";
        protected virtual DateTime mTrigger { get; set; }
        protected bool mIsAlive { get; set; } = true;

        protected Alarm() { }

        public Alarm(DateTime pTrigger)
        {
            this.mId = _ID_COUNTER; Alarm._ID_COUNTER += 1;
            this.mName = $"Alarm #{this.mId}";
            this.mTrigger = pTrigger;
        }
        public Alarm(DateTime pTrigger, string pName)
        {
            this.mId = _ID_COUNTER; Alarm._ID_COUNTER += 1;
            this.mName = pName;
            this.mTrigger = pTrigger;
        }

        /// <summary>
        /// Check if the given DateTime has passed this alarm's DateTime and still within the threshold.
        /// </summary>
        /// <param name="pCurrTime"></param>
        /// <param name="pThresholdSeconds"></param>
        /// <returns></returns>
        public virtual bool CheckAlarm(Plugin pPlugin)
        {
            if (!this.mIsAlive) { return false; }
            //double tDelta = (pCurrTime - this.mTrigger).TotalSeconds;
            //if (tDelta > 0 && tDelta < pThresholdSeconds)
            //    return true;
            //else
            //    return false;
            return true;
        }

        public virtual string Serialize() 
        {
            return "";
        }
    }
}
