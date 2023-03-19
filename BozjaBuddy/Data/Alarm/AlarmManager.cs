using System;
using System.Threading;
using System.Threading.Tasks;
using BozjaBuddy.interop;
using Dalamud.Logging;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace BozjaBuddy.Data.Alarm
{
    public class AlarmManager : IDisposable
    {
        private bool mIsEnabled = false;
        private const int INTERVAL = 1000;
        private bool mIsAppActivated = false;
        private bool mIsAlarmTriggered = false;
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
        public void Start()
        {
            mIsEnabled = true;
            new Thread(
                new ThreadStart(MyAlarm)
            ).Start();
        }
        private void MyAlarm()
        {
            while (mIsEnabled)
            {
                if (Native.ApplicationIsActivated() && !mIsAppActivated)
                {
                    mIsAppActivated = true;
                    //this.mIsAlarmTriggered = true;
                    PluginLog.Information("App is being focused.");
                }
                else if (!Native.ApplicationIsActivated() && mIsAppActivated)
                {
                    mIsAppActivated = false;
                    //this.mIsAlarmTriggered = false;
                    PluginLog.Information("App stops being focused.");
                }
                try
                {
                    if (mIsAlarmTriggered)
                    {
                        if (mOutputDevice != null && mOutputDevice.PlaybackState == PlaybackState.Stopped)
                        {
                            PluginLog.Information($"Alarm sound playing.");
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
