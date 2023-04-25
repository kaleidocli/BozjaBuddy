using BozjaBuddy.Utils;
using Dalamud.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static BozjaBuddy.Data.Alarm.AlarmFateCe;

namespace BozjaBuddy.Data.Alarm
{
    internal class MsgAlarmFateCe : MsgAlarm
    {
        private Plugin? mPlugin = null;
        private string? mTriggerString = null;
        private AlarmFateCe.ExtraCheckOption mExtraOptions = AlarmFateCe.ExtraCheckOption.None;

        protected MsgAlarmFateCe() : base() { }
        public MsgAlarmFateCe(Plugin pPlugin, string pContent, AlarmFateCe.ExtraCheckOption pOptions, string? pTriggerString = null) : base(pContent)
        {
            this.mPlugin = pPlugin;
            this.mExtraOptions = pOptions;
            this.mIsDupable = false;
            this.mTriggerString = pTriggerString;
        }
        public override bool CompareMsg(MsgAlarm pMsgIn)
        {
            if (this.mPlugin == null) return false;

            int tCeId;
            Int32.TryParse(pMsgIn._msg, out tCeId);
            if (tCeId == 0) return false;

            // Vannila check
            if (this.mExtraOptions == ExtraCheckOption.None)
                return base.CompareMsg(pMsgIn);

            // Check: Only Ce
            if (this.mExtraOptions.HasFlag(ExtraCheckOption.OnlyCe)
                && tCeId <= 50
                && !(new int[] { 16, 32, 33, 34}).Contains(tCeId))      // ignoring CLL, Dal, DR, DRS
            {
                // Check: By Zone
                if (this.mExtraOptions.HasFlag(ExtraCheckOption.ByZone))
                {
                    if (this.mTriggerString == null) return false;
                    if (!(this.mPlugin.mBBDataManager.mFates.ContainsKey(tCeId)
                        && this.mPlugin.mBBDataManager.mFates[tCeId].mLocation != null
                        && UtilsGameData.kAreaAndCode[this.mTriggerString] != Location.Area.None
                        && UtilsGameData.kAreaAndCode[this.mTriggerString] == this.mPlugin.mBBDataManager.mFates[tCeId].mLocation!.mAreaFlag))
                    {
                        return false;
                    }
                }
                return true;
            }
            // Check: All FateCe
            else if (this.mExtraOptions.HasFlag(ExtraCheckOption.AllFateCe))
            {
                // Check: By Zone
                if (this.mExtraOptions.HasFlag(ExtraCheckOption.ByZone))
                {
                    if (this.mTriggerString == null) return false;
                    if (!(this.mPlugin.mBBDataManager.mFates.ContainsKey(tCeId)
                        && this.mPlugin.mBBDataManager.mFates[tCeId].mLocation != null
                        && UtilsGameData.kAreaAndCode[this.mTriggerString] != Location.Area.None
                        && UtilsGameData.kAreaAndCode[this.mTriggerString] == this.mPlugin.mBBDataManager.mFates[tCeId].mLocation!.mAreaFlag))
                    {
                        return false;
                    }
                }
                return true;
            }

            return false;
        }
    }
}
