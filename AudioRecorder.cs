// =====================================================================================================================
//    Author: Al-Khafaji, Ali Kifah
//    Date:   21.12.2024
//    Description: A generic audio recorder class that accept IWaveIn implementation like WasapiCapture or WaveInEvent
// =====================================================================================================================

using System;
using NAudio.Wave;

namespace NAudioToolkit
{
    /// <summary>
    /// a generic audio recorder class that accept IWaveIn implementation like WasapiCapture or WaveInEvent
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class AudioRecorder<T> : IAudioRecorder, IDisposable where T: IWaveIn
    {
        #region private members
        // the device used to record audio
        private readonly T _waveIn;
        
        private readonly int _channels;
        
        // buffer to be filled with recorded audio samples.
        //When filled a clone of this buffer shall be passed as an argument to the action "SamplesReady"
        private readonly byte[] _byteBuffer;

        // a counter to keep track of the number of samples added to the buffer
        private int _currentBufferIndex;
        #endregion

        #region public members
        /// <summary>
        /// an action to be invoked when the amount of recorded samples are equal to "AudioRecorder.SampleBlockSize" 
        /// </summary>
        /// 
        public event Action<byte[]> SamplesReady;
        /// <summary>
        /// count of samples to be passed to the action "SamplesReady" when recorded. Value to be set in the constructor. Default value is 1024
        /// </summary>
        public int SampleBlockSize { get; }
        public bool IsRecording { get; private set; }
        #endregion

        #region const/dest/disp
        public AudioRecorder( int sampleRate = 44100, int bitDepth = 16, int channels = 1,
            int sampleBlockSize = 1024, bool useIeeeFloatWaveFormat = false)
        {
            SampleBlockSize = sampleBlockSize;
            _channels = channels;

            // calculate the byte buffer size
            int bytesPerSample = bitDepth / 8;
            _byteBuffer = new byte[SampleBlockSize * bytesPerSample * _channels];

            _waveIn = (T)Activator.CreateInstance(typeof(T)); 

            if (useIeeeFloatWaveFormat)
                _waveIn.WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channels);
            else
                _waveIn.WaveFormat = new WaveFormat(sampleRate, bitDepth, channels);
            _waveIn.DataAvailable += onDataAvailable;
        }

        public AudioRecorder(AudioSettings audioSettings, int sampleBlockSize = 1024)
        {
            SampleBlockSize = sampleBlockSize;
            _channels = audioSettings.Channels;

            // Calculate the byte buffer size
            int bytesPerSample = audioSettings.BitDepth / 8;
            _byteBuffer = new byte[SampleBlockSize * bytesPerSample * _channels];

            _waveIn = (T)Activator.CreateInstance(typeof(T));

            if (audioSettings.IsIEEFloatWave)
                _waveIn.WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(audioSettings.Samplerate, audioSettings.Channels);
            else
                _waveIn.WaveFormat = new WaveFormat(audioSettings.Samplerate, audioSettings.BitDepth, audioSettings.Channels);
            _waveIn.DataAvailable += onDataAvailable;
        }
        
        ~AudioRecorder()
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
            Stop();
            _waveIn.Dispose();
        }
        #endregion

        #region public methods
        public void Start()
        {
            if (IsRecording) return;
            IsRecording = true;
            _waveIn.StartRecording();
        }
        
        public void Stop()
        {
            if (!IsRecording) return;
            IsRecording = false;
            _waveIn.StopRecording();
        }
        #endregion

        #region private methods
        private void onDataAvailable(object sender, WaveInEventArgs e)
        {
            var bytesPerSample = _waveIn.WaveFormat.BitsPerSample / 8;
            var samplesInBuffer = e.BytesRecorded / bytesPerSample;
            
            // a loop to fill the samples buffer
            for (int i = 0; i < samplesInBuffer; i++)
            {
                int offset = i * bytesPerSample;
                Array.Copy(e.Buffer, offset, _byteBuffer, _currentBufferIndex, bytesPerSample);
                _currentBufferIndex += bytesPerSample;
                // invoke the action "SamplesReady" if the buffer is filled with the required number of samples
                if (_currentBufferIndex >= _byteBuffer.Length)
                {
                    SamplesReady?.Invoke((byte[])_byteBuffer.Clone());
                    _currentBufferIndex = 0;
                }
            }
        }
        #endregion

    }// end class

}
