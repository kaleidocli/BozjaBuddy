using System;
using System.Collections.Generic;

namespace BozjaBuddy.Data.Alarm
{
    internal class AlarmFateCe : Alarm
    {
        public new static string kReprString = $"Alarm #{Alarm._ID_COUNTER} [FATECE]";
        public new static string kToolTip = "Alarm for a specific Fate or CE. Can go off once, or every time the FATE or CE comes around.";
        public AlarmFateCe() : base(null, null, kReprString) { }
        public AlarmFateCe(DateTime? pTriggerTime, int? pTriggerInt, string? pName = null)
            : base(pTriggerTime, pTriggerInt, pName ?? kReprString)
        {
        }

        public override bool CheckAlarm(DateTime pCurrTime, Dictionary<string, List<int>> pListeners, int pThresholdSeconds = 10)
        {
            if (!this.mIsAlive || this.mTriggerInt.HasValue)
            {
                this.Kill();
                return false;
            }
            if (pListeners["fatece"].Contains(this.mTriggerInt!.Value))
                return true;
            else
            {
                this.Kill();
                return false;
            }
        }

        public override string Serialize()
        {
            return "";
        }
    }
}
