using Dalamud.Logging;
using System;
using System.Collections.Generic;

namespace BozjaBuddy.Data.Alarm
{
    public abstract class Alarm
    {
        protected static int _ID_COUNTER = 0;
        public const string _kJsonid = "a";
        public virtual string _mJsonId { get; set; } = Alarm._kJsonid;
        public static int kDurationMin = 10;
        public static int kDurationMax = 1200;
        public static int kDefaultDuration = 300;
        public static int kOffsetMin = 10;
        public static int kOffsetMax = 1200;
        public static string kReprString = $"Alarm #{Alarm._ID_COUNTER}";
        public static string kToolTip = "This is truly one of the alarm ever.";
        public static int kRecommendedOffset = 180;
        public int mId { get; set; }
        public string mName;
        private int _mDuration;
        public int mDuration
        {
            get => this._mDuration;
            set { this._mDuration = Alarm.ProcessDuration(value); }
        }
        private int _mOffset;
        public int mOffset
        {
            get => this._mOffset;
            set { this._mOffset = Alarm.ProcessOffset(value); }
        }
        public DateTime? mTimeOfDeath = null;
        public DateTime? mTriggerTime = null;
        public int? mTriggerInt = null;
        public string? mTriggerString = null;
        public bool mIsAlive { get; set; }          // if the Alarm is on to-be-checked (TBC) list
        public bool mIsRevivable { get; set; }      // if the Alarm can be brought back to TBC list 
        public bool mIsAwake { get; set; } = true;          // if the Alarm can be checked while on TBC list 
        public bool mHasBeenRevived { get; set; } = true;

        protected Alarm() : this(null, null, Alarm.kReprString, Alarm.kDefaultDuration)
        {
        }
        protected Alarm(DateTime? pTriggerTime, 
                        int? pTriggerInt, 
                        string pName = "", 
                        int pDuration = -1, 
                        bool pIsAlive = true, 
                        bool pIsRevivable = false,
                        string pTriggerString = "",
                        int pOffset = 0)
        {
            this.mId = Alarm._ID_COUNTER; Alarm._ID_COUNTER += 1;
            this.mTriggerTime = pTriggerTime;
            this.mTriggerInt = pTriggerInt;
            this.mName = pName;
            this.mDuration = pDuration < 0 ? Alarm.kDefaultDuration : pDuration;
            this.mIsAlive = pIsAlive;
            this.mIsRevivable = pIsRevivable;
            this.mTriggerString = pTriggerString;
            this.mOffset = pOffset;
        }
        /// <summary>
        /// Specifically workaround for Generic type init.
        /// Use default contructor first, then init data with this.
        /// <para>Does not increment _ID_COUNTER</para>
        /// </summary>
        public virtual void Init(DateTime? pTriggerTime,
                                int? pTriggerInt,
                                string pName = "",
                                int pDuration = -1,
                                bool pIsAlive = true,
                                bool pIsRevivable = false,
                                string pTriggerString = "",
                                int pOffset = 0)
        {
            this.mTriggerTime = pTriggerTime;
            this.mTriggerInt = pTriggerInt;
            this.mName = pName;
            this.mDuration = pDuration < 0 ? Alarm.kDefaultDuration : pDuration;
            this.mIsAlive = pIsAlive;
            this.mIsRevivable = pIsRevivable;
            this.mTriggerString = pTriggerString;
            this.mOffset = pOffset;
        }

        /// <summary>
        /// Check if the given DateTime has passed this alarm's DateTime and still within the threshold.
        /// <para>Is also used for revive checking. If this return false, then the alarm will be revived.</para>
        /// </summary>
        public abstract bool CheckAlarm(DateTime pCurrTime, Dictionary<string, List<MsgAlarm>> pListeners, int pThresholdSeconds = 10, bool pFlagRevival = false, Plugin? pPlugin = null);
        public virtual void Kill()
        {
            if (this.mIsAlive) this.mTimeOfDeath = DateTime.Now;
            this.mIsAlive = false;
        }
        public virtual void Revive(DateTime pCurrTime, Dictionary<string, List<MsgAlarm>> pListeners, Plugin? pPlugin = null)
        {
            //PluginLog.LogDebug("Trying to revive alarm...");
            if (!this.CheckAlarm(pCurrTime, pListeners, pFlagRevival:true, pPlugin: pPlugin))
            {
                //PluginLog.LogDebug("Revive succeeds!");
                this.mIsAlive = true;
                this.mHasBeenRevived = true;
            }
        }

        public abstract string Serialize();

        public static int GetIdCounter()
        {
            return Alarm._ID_COUNTER;
        }
        /// <summary>Check if offset is within range. If true, return the original. If not, return the correct one.</summary>
        public static int ProcessOffset(int pOffset)
        {
            if (pOffset < Alarm.kOffsetMin) return Alarm.kOffsetMin;
            if (pOffset > Alarm.kOffsetMax) return Alarm.kOffsetMax;
            return pOffset;
        }
        /// <summary>Check if duration is within range. If true, return the original. If not, return the correct one.</summary>
        public static int ProcessDuration(int pOffset)
        {
            if (pOffset < Alarm.kDurationMin) return Alarm.kDurationMin;
            if (pOffset > Alarm.kDurationMax) return Alarm.kDurationMax;
            return pOffset;
        }
        public static void UpdateIdIfGreater(int pNewId)
        {
            Alarm._ID_COUNTER = pNewId > Alarm._ID_COUNTER ? pNewId : Alarm._ID_COUNTER;
        }
    }
}
