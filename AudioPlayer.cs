// ==================================================================================================================================
//    Author: Al-Khafaji, Ali Kifah
//    Date:   21.12.2024
//    Description:  a generic audio player class that accept IWavePlayer implementation like WaveOutEvent and WasapiOut
//                  The player allows playing data in real-time from buffer or from wave file by adding the file path or url
// ==================================================================================================================================

using NAudio.Wave;
using System;
using System.IO;
using System.Net;

namespace NAudioToolkit
{
    /// <summary>
    /// a generic audio player class that accept IWavePlayer implementation like WaveOutEvent  WasapiOut
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class AudioPlayer<T> : IAudioPlayer, IDisposable where T : IWavePlayer
    {
        #region private members
        private IWavePlayer _waveOutDevice;
        // buffer to receive data in real time (like by streaming)
        private BufferedWaveProvider _bufferedWaveProvider;
        // stream to load the audio data from file path or url
        private WaveStream _readerStream;
        private System.Timers.Timer _positionUpdateTimer;
        private int _positionChangeResolutionMs = 100;
        #endregion

        #region public properties/events
        /// <summary>
        /// Action invoked whenever the playback position changes.
        /// </summary>
        public Action<double> OnPositionChanged { get; set; }

        public EventHandler<StoppedEventArgs> PlaybackStopped;
        public int BufferSize
        {
            get
            {
                if (_bufferedWaveProvider == null) return 0;
                return _bufferedWaveProvider.BufferedBytes;
            }
        }

        public PlaybackState PlaybackState => _waveOutDevice.PlaybackState;

        public float Volume
        {
            get
            {
                return _waveOutDevice.Volume;
            }
            set
            {
                _waveOutDevice.Volume = value;
            }
        }

        /// <summary>
        /// Gets the current playback position in seconds.
        /// This value Has no effect when playing from buffer!
        /// </summary>
        public double CurrentPosition
        {
            get
            {
                if (_readerStream == null) return 0;
                return _readerStream.CurrentTime.TotalSeconds;
            }
        }

        /// <summary>
        /// Gets the total duration of the loaded audio file in seconds.
        /// This value Has no effect when playing from buffer!
        /// </summary>
        public double Duration
        {
            get
            {
                if (_readerStream == null) return 0;
                return _readerStream.TotalTime.TotalSeconds;
            }
        }
        /// <summary>
        /// Set whether the player loops after reaching the end of the wave file.
        /// This value Has no effect when playing from buffer!
        /// </summary>
        public bool IsLoop { get; set; }

        #endregion


        #region const/dest/disp
        /// <summary>
        /// Initializes the AudioPlayer with a WAV file from a local file path or a web URL.
        /// </summary>
        /// <param name="source">The path to the WAV file or a web URL.</param>
        public AudioPlayer(string source)
        {
            try
            {
                _waveOutDevice = (T)Activator.CreateInstance(typeof(T));

                if (Uri.TryCreate(source, UriKind.Absolute, out Uri uri) && uri.Scheme.StartsWith("http"))
                    _readerStream = createAudioStreamFromUrl(source);
                else
                    _readerStream = createReaderStreamFromFile(source);

                _waveOutDevice.PlaybackStopped += onPlayingStopped;
                _waveOutDevice.Init(_readerStream);

                // Set up a timer to monitor playback position
                _positionUpdateTimer = new System.Timers.Timer(_positionChangeResolutionMs);
                _positionUpdateTimer.Elapsed += (sender, args) =>
                {
                    OnPositionChanged?.Invoke(CurrentPosition);
                };
            }
            catch (Exception e)
            {
                throw;
            }
        }

        /// <summary>
        /// Initializes the AudioPlayer to play from wave data buffer.
        /// suitable for real-time streaming
        /// </summary>
        /// <param name="sampleRate"></param>
        /// <param name="bitDepth"></param>
        /// <param name="channels"></param>
        /// <param name="bufferDurationInMs"></param>
        /// <param name="useIeeeFloatWaveFormat"></param>
        public AudioPlayer(int sampleRate, int bitDepth = 16, int channels = 1, int bufferDurationInMs = 2000, bool useIeeeFloatWaveFormat = false)
        {
            try
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
            catch (Exception e)
            {
                throw;
            }
        }
        /// <summary>
        /// Initializes the AudioPlayer to play from wave data buffer.
        /// suitable for real-time streaming.     
        /// </summary>
        /// <param name="audioSettings"></param>
        /// <param name="bufferDurationInMs"></param>
        public AudioPlayer(AudioSettings audioSettings, int bufferDurationInMs = 2000)
        {
            try
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
            catch (Exception e)
            {
                throw;
            }
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
            _readerStream?.Dispose();
            _waveOutDevice = null;
        }
        #endregion


        #region private methods
        private void onPlayingStopped(object sender, StoppedEventArgs e)
        {
            PlaybackStopped?.Invoke(this, e);
            if (IsLoop && _readerStream != null)
            {
                if (CurrentPosition >= Duration)
                {
                    SetPosition(0);
                    Play();
                }
            }
            else
            {
                Stop();
            }
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

        private WaveStream createAudioStreamFromUrl(string url)
        {

            using (var webClient = new WebClient())
            {
                var audioData = webClient.DownloadData(url);
                var memoryStream = new MemoryStream(audioData);

                if (url.EndsWith(".aiff", StringComparison.OrdinalIgnoreCase) || url.EndsWith(".aif", StringComparison.OrdinalIgnoreCase))
                    return new AiffFileReader(memoryStream);

                return new WaveFileReader(memoryStream);
            }
        }

        private WaveStream createReaderStreamFromFile(string fileName)
        {
            if (fileName.EndsWith(".wav", StringComparison.OrdinalIgnoreCase))
            {
                return new WaveFileReader(fileName);
            }
            else if (fileName.EndsWith(".aiff", StringComparison.OrdinalIgnoreCase) || fileName.EndsWith(".aif", StringComparison.OrdinalIgnoreCase))
            {
                return new AiffFileReader(fileName);
            }
            else
            {
                // fall back to media foundation reader, see if that can play it
                return new MediaFoundationReader(fileName);
            }
        }

        #endregion


        #region public methods
        public void Play()
        {
            if (PlaybackState == PlaybackState.Playing) return;
            _waveOutDevice?.Play();
            _positionUpdateTimer?.Start();
        }

        public void Pause()
        {
            _waveOutDevice?.Pause();
            _positionUpdateTimer?.Stop();
        }

        public void Stop()
        {
            _positionUpdateTimer?.Stop();
            _waveOutDevice?.Stop();
            _readerStream?.Seek(0, SeekOrigin.Begin);
        }

        public void Restart()
        {
            SetPosition(0);
            _waveOutDevice?.Play();
            _positionUpdateTimer?.Start();
        }
        public void SetPosition(double seconds)
        {
            if (_readerStream == null)
                throw new NotSupportedException("No Seekable stream loaded!");
            long newPosition = (long)(seconds * _readerStream.WaveFormat.AverageBytesPerSecond);
            _readerStream.Position = Math.Min(newPosition, _readerStream.Length);
        }
        /// <summary>
        /// Fast-forwards the playback by the specified number of seconds.
        /// </summary>
        /// <param name="seconds">The number of seconds to fast-forward.</param>
        public void FastForward(double seconds)
        {
            if (_readerStream == null)
                throw new NotSupportedException("No Seekable stream loaded!");
            double newPosition = _readerStream.CurrentTime.TotalSeconds + seconds;
            SetPosition(Math.Min(_readerStream.TotalTime.TotalSeconds, newPosition));
        }

        /// <summary>
        /// Moves the playback backward by the specified number of seconds.
        /// </summary>
        /// <param name="seconds">The number of seconds to rewind.</param>
        public void Rewind(double seconds)
        {
            if (_readerStream == null)
                throw new NotSupportedException("No Seekable stream loaded!");
            double newPosition = _readerStream.CurrentTime.TotalSeconds - seconds;
            SetPosition(Math.Max(0, newPosition)); // Ensure position does not go below 0
        }

        /// <summary>
        /// Add audio data to buffer
        /// </summary>
        /// <param name="audioBytes"></param>
        public void AddAudioData(byte[] audioBytes)
        {
            if (audioBytes == null || audioBytes.Length == 0)
                throw new ArgumentException("Audio data cannot be null or empty.");
            if (_bufferedWaveProvider == null)
                throw new NotSupportedException("Can not add audio data while playing from a file or a url!");

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

