using System;
using System.Collections.Generic;
using System.Linq;
using BozjaBuddy.GUI.Sections;
using Dalamud.Logging;

namespace BozjaBuddy.Data.Alarm
{
    internal class AlarmWeather : Alarm
    {
        public new const string _kJsonid  = "aw";
        public new static string kReprString = "Weather";
        public new static string kToolTip = "Alarm for weather of a specific zone.\n- ONCE: The alarm can only be triggered by the chosen weather at that specific time.\n- REPEAT: The alarm can be triggered every time the chosen type of weather occurs.";
        public override string _mJsonId { get; set; } = AlarmWeather._kJsonid;
        private int mMsgCheckCounter = 0; // check thrice, due to channel swtiching shenaningans and currWeather changing
        public AlarmWeather() : base(null, null, kReprString) { }
        public AlarmWeather(DateTime? pTriggerTime, int? pTriggerWeatherId, string? pName = null, string? pTriggerTerritoryId = null, int pOffset = 0)
            : base(pTriggerTime, pTriggerWeatherId, pName ?? kReprString, 60, true, false, pTriggerTerritoryId ?? "", pOffset)
        {
        }

        public override bool CheckAlarm(DateTime pCurrTime, Dictionary<string, List<MsgAlarm>> pListeners, int pThresholdSeconds = 1380, bool pIsReviving = false, Plugin? pPlugin = null)
        {
            bool tIsInOnceMode = this.mTriggerTime.HasValue && !this.mIsRevivable;
            //PluginLog.LogDebug(String.Format("========== Alarm has isAlive={2} | trgTime={0} | isRevivable={1} | isReviving={3} | hasBeenRezzed={6} | mOffset={4} | isInOnceMode={5}",
            //                                this.mTriggerTime.HasValue ? this.mTriggerTime.ToString() : "null",
            //                                this.mIsRevivable,
            //                                this.mIsAlive,
            //                                pIsReviving,
            //                                this.mOffset,
            //                                tIsInOnceMode,
            //                                this.mHasBeenRevived));
            // Flags validation
            if (!pIsReviving
                && (!this.mIsAlive 
                || !this.mTriggerInt.HasValue 
                || this.mTriggerString == ""))
            {
                //PluginLog.LogDebug($"CHECK FAILED: Alarm is dead (isAlive={this.mIsAlive}) or has no sufficient info (trgInt={this.mTriggerInt.HasValue} trgStr={this.mTriggerString})!");
                //PluginLog.LogDebug("ALARM KILLED: Alarm killed by flags validation!");
                this.Kill();
                return false;
            }

            // Reloading mTriggerTime for repeat-mode (only after init or after being rezzed)
            if (!tIsInOnceMode && this.mHasBeenRevived)
            {
                this.mHasBeenRevived = false;
                this.ReloadTriggerTime();
            }

            double tDelta = (pCurrTime - this.mTriggerTime!.Value).TotalSeconds;
            // Check msg
            if (!this.CheckMsg(pListeners, tDelta, pIsReviving))
            {
                return false;
            }
            // Check if current time has reached, or already past mTriggerTime for an amount of mOffset seconds
            if (Math.Abs(tDelta) > this.mOffset)
            {
                if (tDelta < 0)
                {
                    //PluginLog.LogDebug("CHECK FAILED: Time has not reached");
                    return false;
                }
                else if (tDelta > 0)
                {
                    //PluginLog.LogDebug("CHECK FAILED: Time has already gone past the threshold");
                    //PluginLog.LogDebug("ALARM KILLED: Alarm killed by trgTime!");
                    this.Kill();
                    return false;
                }
            }

            if (pIsReviving)
            {
                //PluginLog.LogDebug($"Revive failed. Either all msg is met, or not out of Offset's range yet (tDelta={tDelta} > tOffset={this.mOffset})");
            }
            else
            {
                //PluginLog.LogDebug($"ALARM KILLED: Alarm killed by final! (tDelta={tDelta} > tOffset={this.mOffset})");
                this.Kill();
            }
            return true;
        }

        private void ReloadTriggerTime()
        {
            //PluginLog.LogDebug("ALARM RELOADED!");
            if (this.mTriggerString != "") 
                this.mTriggerTime = Utils.Utils.ProcessToLocalTime(WeatherBarSection.GetWeatherNext(null, this.mTriggerString!, false).Item2);
        }
        private bool CheckMsg(Dictionary<string, List<MsgAlarm>> pListeners, double tDelta, bool pIsReviving = false)
        {
            foreach (MsgAlarm tMsg in pListeners[tDelta < 0 ? "weatherSequel" : "weather"])     // pick channel "weather" if we're in threshold of pTriggerTime
            {                                                                                   // pick channel "weatherSequel" if we're in offset of pTriggerTime
                MsgAlarmWeather tReceiverMsg = new MsgAlarmWeather(this.mTriggerString ?? "", this.mTriggerInt!.Value);
                if (tMsg.CompareMsg(tReceiverMsg))
                {
                    if (!pIsReviving && this.mMsgCheckCounter < 3)
                    {
                        //PluginLog.LogDebug(String.Format("CHECK VERIFYING: ({6}) Alarm's msgAlarm check is true, but needs verifying! (del={3} ch={0} lstMsg={1} almMsg={2}) (w={4}) (wSq={5})",
                        //                                                    tDelta < 0 ? "weatherSequel" : "weather",
                        //                                                    tMsg._msg,
                        //                                                    tReceiverMsg._msg,
                        //                                                    tDelta,
                        //                                                    String.Join(", ", pListeners["weather"].Select(o => o._msg)),
                        //                                                    String.Join(", ", pListeners["weatherSequel"].Select(o => o._msg)),
                        //                                                    this.mMsgCheckCounter
                        //                                                    ));
                        this.mMsgCheckCounter++;
                        return false;
                    }
                    //PluginLog.LogDebug(String.Format("CHECK SUCCEEDED: Alarm's msgAlarm check is true! (del={3} ch={0} lstMsg={1} almMsg={2}) (w={4}) (wSq={5})",
                    //                                tDelta < 0 ? "weatherSequel" : "weather",
                    //                                tMsg._msg,
                    //                                tReceiverMsg._msg,
                    //                                tDelta,
                    //                                String.Join(", ", pListeners["weather"].Select(o => o._msg)),
                    //                                String.Join(", ", pListeners["weatherSequel"].Select(o => o._msg))
                    //                                ));
                    this.mMsgCheckCounter = 0;
                    return true;
                }
            }
            //PluginLog.LogDebug(String.Format("CHECK FAILED: No msg is true. (del={1} ch={0} (w={2}) (wSq={3})",
            //                                tDelta < 0 ? "weatherSequel" : "weather",
            //                                tDelta,
            //                                String.Join(", ", pListeners["weather"].Select(o => o._msg)),
            //                                String.Join(", ", pListeners["weatherSequel"].Select(o => o._msg))
            //                                ));
            this.mMsgCheckCounter = 0;
            return false;
        }

        public override string Serialize()
        {
            return "";
        }
    }
}
