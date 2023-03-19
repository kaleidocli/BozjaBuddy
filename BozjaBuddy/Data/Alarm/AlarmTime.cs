using System;
using System.Collections.Generic;

namespace BozjaBuddy.Data.Alarm
{
    public class AlarmTime : Alarm
    {
        public new static string kReprString = $"Alarm #{Alarm._ID_COUNTER} [TIME]";
        public new static string kToolTip = "Alarm for a specific time. Can only go off once.";
        public AlarmTime() : base(null, null, kReprString) { }
        public AlarmTime(DateTime? pTriggerTime, int? pTriggerInt, string? pName = null)
            : base(pTriggerTime, pTriggerInt, pName ?? kReprString)
        {
        }

        public override bool CheckAlarm(DateTime pCurrTime, Dictionary<string, List<int>> pListeners, int pThresholdSeconds = 60)
        {
            if (!this.mIsAlive || this.mTriggerTime.HasValue)
            {
                this.Kill();
                return false;
            }
            double tDelta = (pCurrTime - this.mTriggerTime!.Value).TotalSeconds;
            if (tDelta > 0 && tDelta < pThresholdSeconds)
                return true;
            else
            {
                this.Kill();
                return false;
            }
        }
        public override void Revive(DateTime pCurrTime, Dictionary<string, List<int>> pListeners)
        {
            this.mIsRevivable = false;  // Time-based alarm should not be revived. Disable the flag just in case.
        }

        public override string Serialize() 
        {
            return "";
        }
    }
}
