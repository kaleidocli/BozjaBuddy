using System;
using System.Collections.Generic;
using System.Threading;
using Dalamud.Logging;
using NAudio.Wave;

namespace BozjaBuddy.Data.Alarm
{
    public class AlarmManager : IDisposable
    {
        public static Dictionary<string, List<int>> Listeners = new Dictionary<string, List<int>>();
        private List<Alarm> mAlarmList = new List<Alarm>();
        private List<Alarm> mDisposedAlarmList = new List<Alarm>();
        private int mDurationLeft = 0;
        private bool mIsEnabled = false;
        private const int INTERVAL = 1000;
        private bool mIsAppActivated = false;
        private bool mIsAlarmTriggered = false;
        private bool mIsSoundMute = false;
        private Plugin mPlugin;
        private WaveOutEvent? mOutputDevice;
        private LoopStream? mAudioFile;

        public AlarmManager(Plugin pPlugin)
        {
            mPlugin = pPlugin;
            mAudioFile = new LoopStream(new AudioFileReader(mPlugin.DATA_PATHS["alarm_audio"]));
            mOutputDevice = new WaveOutEvent();
            mOutputDevice?.Init(mAudioFile);
        }
        /// <summary>
        /// Primarily for testing
        /// </summary>
        /// <param name="pStatus"></param>
        public void _SetTrigger(bool pStatus)
        {
            this.mIsAlarmTriggered = pStatus;
        }
        public void MuteSound()
        {
            this.mIsSoundMute = true;
        }
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

            // FIXME: add saving to disk
        }

        private void MyAlarm()
        {
            while (mIsEnabled)
            {
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

                foreach (Alarm iAlarm in this.mAlarmList)
                {
                    if (iAlarm.CheckAlarm(DateTime.Now, AlarmManager.Listeners))
                    {
                        this.mDurationLeft = iAlarm.mDuration;
                    }
                    else if (iAlarm.mTimeOfDeath.HasValue && (DateTime.Now - iAlarm.mTimeOfDeath!.Value).TotalSeconds > iAlarm.mDuration)      // x amount of seconds after alarm is dead
                    {
                        if (iAlarm.mIsRevivable)
                        {
                            iAlarm.Revive(DateTime.Now, AlarmManager.Listeners);
                        }
                        else
                        {
                            this.mDisposedAlarmList.Add(iAlarm);
                            this.mAlarmList.Remove(iAlarm);
                        }
                    }
                }
                if (this.mDurationLeft > 0)
                {
                    this.mIsAlarmTriggered = true;
                    this.mDurationLeft -= INTERVAL / 1000;
                }
                else
                {
                    this.mIsAlarmTriggered = false;
                }

                try
                {
                    if (mIsAlarmTriggered)
                    {
                        if (!this.mIsSoundMute && mOutputDevice != null && mOutputDevice.PlaybackState == PlaybackState.Stopped)
                        {
                            PluginLog.Information("Alarm sound playing.");
                            mOutputDevice!.Play();
                        }
                    }
                    else
                    {
                        if (mOutputDevice != null && mOutputDevice.PlaybackState == PlaybackState.Playing)
                        {
                            PluginLog.Information("Alarm sound stopped.");
                            mOutputDevice!.Stop();
                        }

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
            mIsEnabled = false;
            mOutputDevice?.Dispose();
            mOutputDevice = null;
            mAudioFile?.Dispose();
            mAudioFile = null;
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
}
