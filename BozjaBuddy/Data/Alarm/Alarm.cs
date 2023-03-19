using System;
using System.Collections.Generic;

namespace BozjaBuddy.Data.Alarm
{
    public abstract class Alarm
    {
        protected static int _ID_COUNTER = 0;
        public static string kReprString = $"Alarm #{Alarm._ID_COUNTER}";
        public static string kToolTip = "This is truly one of the alarm ever.";
        public int mId { get; set; }
        public string mName;
        public int mDuration;
        public DateTime? mTimeOfDeath = null;
        public DateTime? mTriggerTime = null;
        public int? mTriggerInt = null;
        public bool mIsAlive { get; set; }          // if the Alarm is on to-be-checked (TBC) list
        public bool mIsRevivable { get; set; }      // if the Alarm can be brought back to TBC list 
        public bool mIsAwake { get; set; }          // if the Alarm can be checked while on TBC list 

        protected Alarm() : this(null, null, Alarm.kReprString)
        {
        }
        protected Alarm(DateTime? pTriggerTime, 
                        int? pTriggerInt, 
                        string pName = "", 
                        int pDuration = 60, 
                        bool pIsAlive = true, 
                        bool pIsRevivable = false)
        {
            this.mId = Alarm._ID_COUNTER; Alarm._ID_COUNTER += 1;
            this.mTriggerTime = pTriggerTime;
            this.mTriggerInt = pTriggerInt;
            this.mName = pName;
            this.mDuration = pDuration;
            this.mIsAlive = pIsAlive;
            this.mIsRevivable = pIsRevivable;
        }
        /// <summary>
        /// Specifically workaround for Generic type init.
        /// Use default contructor first, then init data with this.
        /// <para>Does not increment _ID_COUNTER</para>
        /// </summary>
        public virtual void Init(DateTime? pTriggerTime,
                                int? pTriggerInt,
                                string pName = "",
                                int pDuration = 60,
                                bool pIsAlive = true,
                                bool pIsRevivable = false)
        {
            this.mTriggerTime = pTriggerTime;
            this.mTriggerInt = pTriggerInt;
            this.mName = pName;
            this.mDuration = pDuration;
            this.mIsAlive = pIsAlive;
            this.mIsRevivable = pIsRevivable;
        }

        /// <summary>
        /// Check if the given DateTime has passed this alarm's DateTime and still within the threshold.
        /// <para>Is also used for revive checking. If this return false, then the alarm will be revived.</para>
        /// </summary>
        public abstract bool CheckAlarm(DateTime pCurrTime, Dictionary<string, List<int>> pListeners, int pThresholdSeconds = 10);
        public virtual void Kill()
        {
            this.mTimeOfDeath = DateTime.Now;
            this.mIsAlive = false;
        }
        public virtual void Revive(DateTime pCurrTime, Dictionary<string, List<int>> pListeners)
        {
            if (!this.CheckAlarm(pCurrTime, pListeners))
            {
                this.mIsAlive = true;
            }
        }
        public virtual void SetAwake(bool pIsAwake)
        {
            this.mIsAwake = pIsAwake;
        }

        public abstract string Serialize();
    }
}
