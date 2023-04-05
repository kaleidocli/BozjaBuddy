using System;
using System.Collections.Generic;
using System.IO;
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
        public static Dictionary<string, List<MsgAlarm>> Listeners = new Dictionary<string, List<MsgAlarm>>();
        private const int INTERVAL = 1000;
        private List<Alarm> mAlarmList = new List<Alarm>();
        private List<Alarm> mExpiredAlarmList = new List<Alarm>();
        private int mDurationLeft = 0;
        private int mActiveAndAwakeAlarmCount = 0;
        private bool mIsEnabled = false;
        private bool mIsAppActivated = false;
        private bool mIsAlarmTriggered = false;
        private bool mIsSoundMute = false;
        private Task mSoundPlayerTask = Task.CompletedTask;
        private CancellationTokenSource mSoundPlayerTaskCTS = new();
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

            while (mIsEnabled)
            {
                if (this.mActiveAndAwakeAlarmCount == 0 && !(this.mDurationLeft > 0))
                {
                    Thread.Sleep(INTERVAL);
                    continue;
                }
                if (tCounter % 5 == 0) // refresh every 5 secs
                    WeatherBarSection._updateWeatherCurr(mPlugin);
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
                foreach (Alarm iAlarm in this.mAlarmList)
                {
                    if (!iAlarm.mIsAwake) continue;
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
                    CancellationToken tToken = this.mSoundPlayerTaskCTS.Token;
                    if (this.mIsAlarmTriggered && this.mSoundPlayerTask!.Status != TaskStatus.Running && !this.mIsSoundMute)
                    {
                        PluginLog.Information("Alarm sound playing.");
                        this.mSoundPlayerTask = new Task(
                                () => SoundPlayer(this.mPlugin.Configuration.mAudioPath, this.mPlugin.Configuration.mAudioVolume, tToken),
                                this.mSoundPlayerTaskCTS.Token);
                        this.mSoundPlayerTask.Start();
                    }
                    else if (this.mSoundPlayerTask!.Status == TaskStatus.Running && (!this.mIsAlarmTriggered || this.mIsSoundMute))
                    {
                        PluginLog.Information("Alarm sound stopped.");
                        this.mSoundPlayerTaskCTS.Cancel();
                        this.mSoundPlayerTask.Wait();
                        this.mSoundPlayerTaskCTS.Dispose();
                        this.mSoundPlayerTaskCTS = new CancellationTokenSource();
                    }
                }
                catch (Exception e)
                {
                    PluginLog.Error(e.Message + "\n" + e.StackTrace ?? "");
                }
                Thread.Sleep(INTERVAL);
            }

        }

        // https://github.com/goatcorp/DalamudPluginsD17/pull/1155
        // https://github.com/goatcorp/DalamudPluginsD17/pull/112#issuecomment-1222769995
        private void SoundPlayer(string pPath, float pVolume, CancellationToken pToken)
        {
            pToken.ThrowIfCancellationRequested();

            System.Guid tOutputDevice = DirectSoundOut.DSDEVID_DefaultPlayback;
            LoopStream tAudioReader;
            // Audio stuff
            try
            {
                tAudioReader = new LoopStream(new MediaFoundationReader(pPath));
            }
            catch (Exception e)
            {
                PluginLog.LogError($"Path might be invalid: {pPath}\n{e.Message}");
                return;
            }
            using var tChannel = new WaveChannel32(tAudioReader)
            {
                Volume = pVolume,
                PadWithZeroes = false,
            };

            using (tAudioReader)
            {
                using var tOutput = new DirectSoundOut(tOutputDevice);
                try
                {
                    tOutput.Init(tChannel);
                    tOutput.Play();
                    while (tOutput.PlaybackState == PlaybackState.Playing)
                    {
                        if (pToken.IsCancellationRequested)
                        {
                            tOutput.Stop();
                            pToken.ThrowIfCancellationRequested();
                            break;
                        }
                        Thread.Sleep(500);
                    }
                }
                catch (Exception e)
                {
                    PluginLog.LogError(e.Message);
                    return;
                }
            }
        }

        public void Dispose()
        {
            this.mIsEnabled = false;
            this.mSoundPlayerTaskCTS.Dispose();
        }
    }

    /// <summary>
    /// Stream for looping playback
    /// </summary>
    public class LoopStream : WaveStream
    {
        WaveStream sourceStream;

        /// <summary>
        /// Creates a new Loop stream
        /// </summary>
        /// <param name="sourceStream">The stream to read from. Note: the Read method of this stream should return 0 when it reaches the end
        /// or else we will not loop to the start again.</param>
        public LoopStream(WaveStream sourceStream)
        {
            this.sourceStream = sourceStream;
            EnableLooping = true;
        }

        /// <summary>
        /// Use this to turn looping on or off
        /// </summary>
        public bool EnableLooping { get; set; }

        /// <summary>
        /// Return source stream's wave format
        /// </summary>
        public override WaveFormat WaveFormat
        {
            get { return sourceStream.WaveFormat; }
        }

        /// <summary>
        /// LoopStream simply returns
        /// </summary>
        public override long Length
        {
            get { return sourceStream.Length; }
        }

        /// <summary>
        /// LoopStream simply passes on positioning to source stream
        /// </summary>
        public override long Position
        {
            get { return sourceStream.Position; }
            set { sourceStream.Position = value; }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int totalBytesRead = 0;

            while (totalBytesRead < count)
            {
                int bytesRead = sourceStream.Read(buffer, offset + totalBytesRead, count - totalBytesRead);
                if (bytesRead == 0 || sourceStream.Position > sourceStream.Length)
                {
                    if (sourceStream.Position == 0 || !EnableLooping)
                    {
                        // something wrong with the source stream
                        break;
                    }
                    // loop
                    sourceStream.Position = 0;
                }
                totalBytesRead += bytesRead;
            }
            return totalBytesRead;
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
