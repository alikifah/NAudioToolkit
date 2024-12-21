// =====================================================================================================================
//    Author: Al-Khafaji, Ali Kifah
//    Date:   21.12.2024
//    Description:  a generic audio player class that accept IWavePlayer implementation like WaveOutEvent and WasapiOut
// =====================================================================================================================

using NAudio.Wave;
using System;
using System.Threading.Tasks;

namespace NAudioToolkit
{
    /// <summary>
    /// a generic audio player class that accept IWavePlayer implementation like WaveOutEvent and WasapiOut
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class AudioPlayer<T> : IAudioPlayer, IDisposable where T: IWavePlayer
    {
        #region private members
        private IWavePlayer _waveOutDevice;
        private BufferedWaveProvider _bufferedWaveProvider;
        #endregion

        #region public properties/events
        public EventHandler<StoppedEventArgs> PlaybackStopped;
        public int BufferSize
        {
            get
            {
                if (_waveOutDevice == null)
                    throw new ObjectDisposedException(nameof(AudioPlayer<T>));
                return _bufferedWaveProvider.BufferedBytes;
            }
        }

        public PlaybackState PlaybackState
        {
            get
            {
                if (_waveOutDevice == null)
                    throw new ObjectDisposedException(nameof(AudioPlayer<T>));
                return _waveOutDevice.PlaybackState;
            }
        }

        public float Volume
        {
            get
            {
                if (_waveOutDevice == null)
                    throw new ObjectDisposedException(nameof(AudioPlayer<T>));
                return _waveOutDevice.Volume;
            }
            set
            {
                if (_waveOutDevice == null)
                    throw new ObjectDisposedException(nameof(AudioPlayer<T> ));
                _waveOutDevice.Volume = value;
            }
        }
        #endregion


        #region const/dest/disp
        public AudioPlayer(int sampleRate, int bitDepth = 16, int channels = 1, int bufferDurationInMs = 2000, bool useIeeeFloatWaveFormat = false)
        {
            _waveOutDevice = (T)Activator.CreateInstance(typeof(T)); 
            WaveFormat waveFormat;
            if (useIeeeFloatWaveFormat)
                waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channels);
            else
                waveFormat = new WaveFormat(sampleRate, bitDepth, channels);

            _bufferedWaveProvider = new BufferedWaveProvider(waveFormat);
            _bufferedWaveProvider.DiscardOnBufferOverflow = true;
            _bufferedWaveProvider.BufferLength = calculateWaveDataLength(sampleRate, bitDepth, channels, bufferDurationInMs + 1000);
            _waveOutDevice.Init(_bufferedWaveProvider);
            _waveOutDevice.PlaybackStopped += onPlayingStopped;
        }

        public AudioPlayer(AudioSettings audioSettings, int bufferDurationInMs = 2000)
        {
            _waveOutDevice = (T)Activator.CreateInstance(typeof(T)); 
            WaveFormat waveFormat;
            if (audioSettings.IsIEEFloatWave)
                waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(audioSettings.Samplerate, audioSettings.Channels);
            else
                waveFormat = new WaveFormat(audioSettings.Samplerate, audioSettings.BitDepth, audioSettings.Channels);

            _bufferedWaveProvider = new BufferedWaveProvider(waveFormat);
            _bufferedWaveProvider.DiscardOnBufferOverflow = true;
            _bufferedWaveProvider.BufferLength = calculateWaveDataLength(audioSettings.Samplerate, audioSettings.BitDepth, audioSettings.Channels, bufferDurationInMs);
            _waveOutDevice.Init(_bufferedWaveProvider);
            _waveOutDevice.PlaybackStopped += onPlayingStopped;
        }

        ~AudioPlayer()
        {
            dispose();
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            dispose();
        }

        private void dispose()
        {
            _waveOutDevice.PlaybackStopped -= onPlayingStopped;
            _waveOutDevice.Dispose();
            _waveOutDevice = null;
        }
        #endregion


        #region private methods
        private void onPlayingStopped(object sender, StoppedEventArgs e)
        {
            PlaybackStopped?.Invoke(this, e);
        }

        private int calculateWaveDataLength(int sampleRate, int bitDepth, int channelCount, int durationInMilliSeconds)
        {
            if (sampleRate <= 0 || bitDepth <= 0 || channelCount <= 0 || durationInMilliSeconds <= 0)
                throw new ArgumentException("All parameters must be positive values.");

            int bytesPerSample = bitDepth / 8;
            int totalSamples = sampleRate * durationInMilliSeconds / 1000;
            int totalBytes = totalSamples * bytesPerSample * channelCount;

            return totalBytes;
        }
        #endregion


        #region public methods
        public void Play()
        {
            if (PlaybackState == PlaybackState.Playing) return;
            if (_waveOutDevice == null)
                throw new ObjectDisposedException(nameof(AudioPlayer<T>));
            _waveOutDevice?.Play();
        }

        public async Task Play(int delayMs)
        {
            if (PlaybackState == PlaybackState.Playing) return;
            if (_waveOutDevice == null)
                throw new ObjectDisposedException(nameof(AudioPlayer<T>));
            await Task.Delay(delayMs);
            _waveOutDevice?.Play();
        }

        public void Pause()
        {
            if (_waveOutDevice == null)
                throw new ObjectDisposedException(nameof(AudioPlayer<T>));
            _waveOutDevice.Pause();
        }

        public void Stop()
        {
            if (_waveOutDevice == null)
                throw new ObjectDisposedException(nameof(AudioPlayer<T>));
            _waveOutDevice?.Stop();
        }

        public void AddAudioData(byte[] audioBytes)
        {
            if (audioBytes == null || audioBytes.Length == 0)
                throw new ArgumentException("Audio data cannot be null or empty.");
            _bufferedWaveProvider.AddSamples(audioBytes, 0, audioBytes.Length);
        }

        public void AddAudioData(short[] audioShorts)
        {
            if (audioShorts == null || audioShorts.Length == 0)
                throw new ArgumentException("Audio data cannot be null or empty.");

            byte[] audioBytes = new byte[audioShorts.Length * 2];
            Buffer.BlockCopy(audioShorts, 0, audioBytes, 0, audioBytes.Length);

            AddAudioData(audioBytes);
        }

        public void AddAudioData(float[] audioData)
        {
            if (audioData == null || audioData.Length == 0)
                throw new ArgumentException("Audio data cannot be null or empty.");

            byte[] audioBytes = new byte[audioData.Length * 4];
            Buffer.BlockCopy(audioData, 0, audioBytes, 0, audioBytes.Length);
            AddAudioData(audioBytes);
        }
        #endregion

    }// end class

}
