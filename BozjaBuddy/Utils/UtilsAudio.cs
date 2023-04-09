using BozjaBuddy.Data.Alarm;
using Dalamud.Logging;
using NAudio.Wave;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BozjaBuddy.Utils.UtilsAudio
{
    internal class AudioPlayer : IDisposable
    {
        private CancellationTokenSource mSoundPlayerTaskCTS = new();
        private Task mSoundPlayerTask = Task.CompletedTask;

        public void StartAudio(string pPath, float pVolume, bool pIsInterrupting = false, bool pIsLooping = true)
        {
            if (this.mSoundPlayerTask!.Status == TaskStatus.Running)
            {
                if (!pIsInterrupting) return;
                this.StopAudio();
            }

            //PluginLog.Information("Alarm sound playing.");
            CancellationToken tToken = this.mSoundPlayerTaskCTS.Token;
            this.mSoundPlayerTask = new Task(
                    () => this.SoundPlayer(pPath, pVolume, tToken, pIsLooping: pIsLooping),
                    this.mSoundPlayerTaskCTS.Token);
            this.mSoundPlayerTask.Start();
        }
        public void StopAudio()
        {
            //PluginLog.Information("Alarm sound stopped.");
            this.mSoundPlayerTaskCTS.Cancel();
            this.mSoundPlayerTask.Wait();
            this.mSoundPlayerTaskCTS.Dispose();
            this.mSoundPlayerTaskCTS = new CancellationTokenSource();
        }

        // https://github.com/goatcorp/DalamudPluginsD17/pull/1155
        // https://github.com/goatcorp/DalamudPluginsD17/pull/112#issuecomment-1222769995
        private void SoundPlayer(string pPath, float pVolume, CancellationToken pToken, bool pIsLooping = true)
        {
            pToken.ThrowIfCancellationRequested();

            System.Guid tOutputDevice = DirectSoundOut.DSDEVID_DefaultPlayback;
            LoopStream tAudioReader;
            // Audio stuff
            try
            {
                tAudioReader = new LoopStream(new MediaFoundationReader(pPath));
                tAudioReader.EnableLooping = pIsLooping;
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
}
