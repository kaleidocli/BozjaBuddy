using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BozjaBuddy.GUI.Sections;
using Dalamud.Logging;
using NAudio.Wave;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BozjaBuddy.Data.Alarm
{
    public class AlarmManager : IDisposable
    {
        private static Dictionary<string, List<MsgAlarm>> Listeners = new();
        private static Dictionary<string, HashSet<MsgAlarm>> ListenersNondupe = new();
        private const int INTERVAL = 1000;
        private List<Alarm> mAlarmList = new List<Alarm>();
        private List<Alarm> mExpiredAlarmList = new List<Alarm>();
        private int mDurationLeft = 0;
        private int mActiveAndAwakeAlarmCount = 0;
        private bool mIsEnabled = false;
        private bool mIsAppActivated = false;
        private bool mIsAlarmTriggered = false;
        private bool mIsSoundMute = false;
        private Utils.UtilsAudio.AudioPlayer mAudioPlayer = new();
        private Plugin mPlugin;

        public AlarmManager(Plugin pPlugin)
        {
            mPlugin = pPlugin;
            this.LoadAlarmListsFromDisk();
        }
        /// <summary>
        /// Primarily for testing
        /// </summary>
        /// <param name="pStatus"></param>
        public void _SetTrigger(bool pStatus)
        {
            this.mIsAlarmTriggered = pStatus;
        }
        public void MuteSound() { this.mIsSoundMute = true; }
        public void UnmuteSound() { this.mIsSoundMute = false; }
        public bool GetMuteStatus() => this.mIsSoundMute;
        public bool GetTriggerStatus() => this.mIsAlarmTriggered;
        public void Start()
        {
            mIsEnabled = true;
            new Thread(
                new ThreadStart(MyAlarm)
            ).Start();
        }
        public void AddAlarm(Alarm pAlarm)
        {
            mAlarmList.Add(pAlarm);
            this.mActiveAndAwakeAlarmCount++;

            // Save to disk
            this.SaveAlarmListsToDisk();
        }
        public void RemoveAlarm(int pAlarmId)
        {
            // Active alarm
            Alarm? pTargetAlarm = null;
            foreach (Alarm iAlarm in this.mAlarmList)
            {
                if (iAlarm.mId == pAlarmId)
                {
                    pTargetAlarm = iAlarm;
                    break;
                }
            }
            if (pTargetAlarm != null)
            {
                this.mAlarmList.Remove(pTargetAlarm);
                if (pTargetAlarm.mIsAwake) this.mActiveAndAwakeAlarmCount--;
                return;
            }
            // Expired alarm
            foreach (Alarm iAlarm in this.mExpiredAlarmList)
            {
                if (iAlarm.mId == pAlarmId)
                {
                    pTargetAlarm = iAlarm;
                    break;
                }
            }
            if (pTargetAlarm != null)
            {
                this.mExpiredAlarmList.Remove(pTargetAlarm);
                if (pTargetAlarm.mIsAwake) this.mActiveAndAwakeAlarmCount--;
                return;
            }

            // Save to disk
            this.SaveAlarmListsToDisk();
        }
        public void RemoveAlarm(Alarm pAlarm)
        {
            if (this.mAlarmList.Remove(pAlarm) && pAlarm.mIsAwake)
                this.mActiveAndAwakeAlarmCount--;
            if (this.mExpiredAlarmList.Remove(pAlarm) && pAlarm.mIsAwake)
                this.mActiveAndAwakeAlarmCount--;

            // Save to disk
            this.SaveAlarmListsToDisk();
        }
        /// <summary> Active alarm only </summary>
        public void WakeAlarm(int pAlarmId)
        {
            // Active alarm
            foreach (Alarm iAlarm in this.mAlarmList)
            {
                if (iAlarm.mId == pAlarmId)
                {
                    if (!iAlarm.mIsAwake)
                    {
                        iAlarm.mIsAwake = true;
                        this.mActiveAndAwakeAlarmCount++;
                    }
                    break;
                }
            }
        }
        public void WakeAlarm(Alarm pAlarm)
        {
            if (!pAlarm.mIsAwake)
            {
                pAlarm.mIsAwake = true;
                this.mActiveAndAwakeAlarmCount++;
            }
        }
        /// <summary> Active alarm only </summary>
        public void SleepAlarm(int pAlarmId)
        {
            // Active alarm
            foreach (Alarm iAlarm in this.mAlarmList)
            {
                if (iAlarm.mId == pAlarmId)
                {
                    if (iAlarm.mIsAwake)
                    {
                        iAlarm.mIsAwake = false;
                        this.mActiveAndAwakeAlarmCount--;
                    }
                    break;
                }
            }
        }
        public void SleepAlarm(Alarm pAlarm)
        {
            if (pAlarm.mIsAwake)
            {
                pAlarm.mIsAwake = false;
                this.mActiveAndAwakeAlarmCount--;
            }
        }
        public void ExpireAlarm(Alarm pAlarm)
        {
            this.mExpiredAlarmList.Add(pAlarm);
            this.mAlarmList.Remove(pAlarm);
            this.mActiveAndAwakeAlarmCount -= 1;
        }
        public void UnexpireAlarm(Alarm pAlarm)
        {
            pAlarm.mIsAlive = true;
            this.mAlarmList.Add(pAlarm);
            this.mExpiredAlarmList.Remove(pAlarm);
            this.mActiveAndAwakeAlarmCount += 1;
        }
        private void SaveAlarmListsToDisk()
        {
            List<List<Alarm>> tAlarmLists = new List<List<Alarm>> { this.mAlarmList, this.mExpiredAlarmList };
            string tJson = JsonConvert.SerializeObject(tAlarmLists);
            //PluginLog.LogDebug($">>> SAVING TO DISK: {tJson}");
            File.WriteAllText(this.mPlugin.DATA_PATHS["alarm.json"], tJson);
        }
        private void LoadAlarmListsFromDisk()
        {
            JsonConverter[] tConverters = { new AlarmConverter() };
            List<List<Alarm>>? tAlarmLists = JsonConvert.DeserializeObject<List<List<Alarm>>>(
                        File.ReadAllText(this.mPlugin.DATA_PATHS["alarm.json"]),
                        tConverters
                    );
            if (tAlarmLists == null)
            {
                //PluginLog.LogDebug(">>> A_MNG: Unable to load alarm from disk.");
                return;
            }
            this.mAlarmList = tAlarmLists![0];
            this.mExpiredAlarmList = tAlarmLists![1];
            // Update id count
            foreach (Alarm iAlarm in this.mAlarmList)
            {
                Alarm.UpdateIdIfGreater(iAlarm.mId);
            }
            foreach (Alarm iAlarm in this.mExpiredAlarmList)
            {
                Alarm.UpdateIdIfGreater(iAlarm.mId);
            }
            this.RefreshAAAAlarmCount();
        }
        private void RefreshAAAAlarmCount()
        {
            // Recalculated mActiveAndAwakeAlarmCount
            foreach (Alarm iAlarm in this.mAlarmList)
            {
                if (iAlarm.mIsAwake) { this.mActiveAndAwakeAlarmCount++; }
            }
        }
        public int GetAAAAlarmCount() => this.mActiveAndAwakeAlarmCount;
        public List<Alarm> GetAlarms() => this.mAlarmList;
        public List<Alarm> GetDisposedAlarms() => this.mExpiredAlarmList;
        public int GetDurationLeft() => this.mDurationLeft;

        private void MyAlarm()
        {
            int tCounter = 0;
            bool tNeedToUnrequest = false;       // mainly for unrequesting GUIAssist

            while (mIsEnabled)
            {
                if (this.mActiveAndAwakeAlarmCount == 0 && !(this.mDurationLeft > 0))
                {
                    if (tNeedToUnrequest)
                    {
                        this.mPlugin.GUIAssistManager.UnrequestOption(this.GetHashCode(), GUI.GUIAssist.GUIAssistManager.GUIAssistOption.MycInfoBox);
                        tNeedToUnrequest = false;
                    }
                    Thread.Sleep(INTERVAL);
                    continue;
                }
                if (tCounter % 5 == 0) // refresh every 5 secs
                {
                    WeatherBarSection._updateWeatherCurr(mPlugin);
                    BBDataManager.UpdateAllFateStatus(mPlugin);
                }
                tCounter = tCounter == 5 ? 0 : tCounter + 1;
                //if (Native.ApplicationIsActivated() && !mIsAppActivated)
                //{
                //    mIsAppActivated = true;
                //    //this.mIsAlarmTriggered = true;
                //    PluginLog.Information("App is being focused.");
                //}
                //else if (!Native.ApplicationIsActivated() && mIsAppActivated)
                //{
                //    mIsAppActivated = false;
                //    //this.mIsAlarmTriggered = false;
                //    PluginLog.Information("App stops being focused.");
                //}
                List<Alarm> tTempAlarmBin = new List<Alarm>();
                int tFateCeAlarmCount = 0;
                foreach (Alarm iAlarm in this.mAlarmList)
                {
                    if (!iAlarm.mIsAwake) continue;
                    if (iAlarm.GetType() == typeof(AlarmFateCe) && iAlarm.mTriggerInt.HasValue && iAlarm.mTriggerInt < 50) tFateCeAlarmCount++;
                    // check if alarm should be killed. If yes, refresh the duration => trigger the sound player
                    if (iAlarm.CheckAlarm(DateTime.Now, AlarmManager.Listeners, pPlugin: this.mPlugin))
                    {
                        this.mDurationLeft = iAlarm.mDuration;
                        //PluginLog.LogDebug($"========== A_MNG: Refreshing duration (duration={iAlarm.mDuration})");
                    }
                    // alarm is dead. Check if the grace period is finished. Revive if possible, else throw to the bin.
                    else if (!iAlarm.mIsAlive && iAlarm.mTimeOfDeath.HasValue && (DateTime.Now - iAlarm.mTimeOfDeath!.Value).TotalSeconds > iAlarm.mDuration)      // x amount of seconds after alarm is dead
                    {
                        //PluginLog.LogDebug($"ALARM's has timeOfDeath? {iAlarm.mTimeOfDeath.HasValue} ({iAlarm.mTimeOfDeath})");
                        if (iAlarm.mIsRevivable)
                        {
                            iAlarm.Revive(DateTime.Now, AlarmManager.Listeners, pPlugin: this.mPlugin);
                        }
                        else
                        {
                            tTempAlarmBin.Add(iAlarm);
                        }
                    }
                    //PluginLog.LogDebug($">>> A_MNG: tOD={iAlarm.mTimeOfDeath?.ToString()} timeSinceDeath={(iAlarm.mTimeOfDeath.HasValue ? (DateTime.Now - iAlarm.mTimeOfDeath!.Value).TotalSeconds : 0)} mDuration={iAlarm.mDuration}");
                }
                foreach (Alarm iAlarm in tTempAlarmBin)
                {
                    this.ExpireAlarm(iAlarm);
                }
                // GUIAssist stuff
                if (tFateCeAlarmCount > 0)
                {
                    this.mPlugin.GUIAssistManager.RequestOption(this.GetHashCode(), GUI.GUIAssist.GUIAssistManager.GUIAssistOption.MycInfoBox);
                    tNeedToUnrequest = true;
                }
                else if (tNeedToUnrequest)
                {
                    this.mPlugin.GUIAssistManager.UnrequestOption(this.GetHashCode(), GUI.GUIAssist.GUIAssistManager.GUIAssistOption.MycInfoBox);
                    tNeedToUnrequest = false;
                }   

                if (this.mDurationLeft > 0)
                {
                    this.mIsAlarmTriggered = true;
                    this.mDurationLeft -= INTERVAL / 1000;
                }
                else
                {
                    this.mIsAlarmTriggered = false;
                    this.UnmuteSound();
                }

                try
                {
                    if (this.mIsAlarmTriggered && !this.mIsSoundMute)
                    {
                        this.mAudioPlayer.StartAudio(this.mPlugin.Configuration.mAudioPath ?? "", this.mPlugin.Configuration.mAudioVolume);
                    }
                    else if (!this.mIsAlarmTriggered || (this.mIsAlarmTriggered && this.mIsSoundMute))
                    {
                        this.mAudioPlayer.StopAudio();
                    }
                }
                catch (Exception e)
                {
                    PluginLog.Error(e.Message + "\n" + e.StackTrace ?? "");
                }
                Thread.Sleep(INTERVAL);
            }

        }

        public void Dispose()
        {
            this.mIsEnabled = false;
            this.mAudioPlayer.Dispose();
        }

        public static void NotifyListener(string pChannel, MsgAlarm pMsg, int pCapacityToReachAndClearChannel = 0)
        {
            if (pMsg.mIsDupable)
            {
                if (!AlarmManager.Listeners.ContainsKey(pChannel))
                {
                    AlarmManager.Listeners.Add(pChannel, new List<MsgAlarm>());
                }
                if (pCapacityToReachAndClearChannel != 0 && AlarmManager.Listeners[pChannel].Count >= pCapacityToReachAndClearChannel)
                {
                    AlarmManager.Listeners[pChannel].Clear();
                }
                AlarmManager.Listeners[pChannel].Add(pMsg);
            }
            else
            {
                if (!AlarmManager.ListenersNondupe.ContainsKey(pChannel))
                {
                    AlarmManager.ListenersNondupe.Add(pChannel, new HashSet<MsgAlarm>());
                }
                else if (AlarmManager.ListenersNondupe[pChannel].Contains(pMsg))
                {
                    return;
                }
                if (pCapacityToReachAndClearChannel != 0 && AlarmManager.ListenersNondupe[pChannel].Count >= pCapacityToReachAndClearChannel)
                {
                    AlarmManager.ListenersNondupe[pChannel].Clear();
                }
                AlarmManager.ListenersNondupe[pChannel].Add(pMsg);
            }
        }
        public static bool CheckMsg(string pChannel, MsgAlarm pMsgToCheck)
        {
            if (pMsgToCheck.mIsDupable)
            {
                if (!AlarmManager.Listeners.ContainsKey(pChannel)) return false;
                foreach (MsgAlarm iMsg in AlarmManager.Listeners[pChannel])
                {
                    if (iMsg.CompareMsg(pMsgToCheck)) return true;
                }
            }
            else
            {
                if (!AlarmManager.ListenersNondupe.ContainsKey(pChannel)) return false;
                foreach (MsgAlarm iMsg in AlarmManager.ListenersNondupe[pChannel])
                {
                    if (iMsg.CompareMsg(pMsgToCheck)) return true;
                }
            }
            return false;
        }
        public static void RemoveMsg(string pChannel, MsgAlarm pMsgToRemove)
        {
            if (pMsgToRemove.mIsDupable)
            {
                if (!AlarmManager.Listeners.ContainsKey(pChannel)) return;
                AlarmManager.Listeners[pChannel] = AlarmManager.Listeners[pChannel]
                                                    .Select(o => o)
                                                    .Where(o => !o.CompareMsg(pMsgToRemove))
                                                    .ToList();
            }
            else
            {
                if (!AlarmManager.ListenersNondupe.ContainsKey(pChannel)) return;
                AlarmManager.ListenersNondupe[pChannel] = AlarmManager.ListenersNondupe[pChannel]
                                                    .Select(o => o)
                                                    .Where(o => !o.CompareMsg(pMsgToRemove))
                                                    .ToHashSet();
            }
        }
    }

    // https://blog.codeinside.eu/2015/03/30/json-dotnet-deserialize-to-abstract-class-or-interface/
    public class AlarmConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return (objectType == typeof(Alarm));
        }

        public override object ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            JObject jo = JObject.Load(reader);
            if (jo["_mJsonId"]!.Value<string>() == AlarmWeather._kJsonid)
                return jo.ToObject<AlarmWeather>(serializer)!;
            else if (jo["_mJsonId"]!.Value<string>() == AlarmTime._kJsonid)
                return jo.ToObject<AlarmTime>(serializer)!;
            else if (jo["_mJsonId"]!.Value<string>() == AlarmFateCe._kJsonid)
                return jo.ToObject<AlarmFateCe>(serializer)!;
            else
                return jo.ToObject<Alarm>(serializer)!;
        }

        public override bool CanWrite
        {
            get { return false; }
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
