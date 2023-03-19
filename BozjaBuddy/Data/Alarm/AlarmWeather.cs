﻿using System;
using System.Collections.Generic;

namespace BozjaBuddy.Data.Alarm
{
    internal class AlarmWeather : Alarm
    {
        public new static string kReprString = $"Alarm #{Alarm._ID_COUNTER} [WEATHER]";
        public new static string kToolTip = "Alarm for a specific weather. Can go off once, or every time the weather comes around.";
        public AlarmWeather() : base(null, null, kReprString) { }
        public AlarmWeather(DateTime? pTriggerTime, int? pTriggerInt, string? pName = null)
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
            if (pListeners["weather"].Contains(this.mTriggerInt!.Value))
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