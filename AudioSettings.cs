
namespace NAudioToolkit
{
    /// <summary>
    /// Audio settings used with AudioRecorder and AudioPlayer
    /// </summary>
    public struct AudioSettings
    {
        public int Samplerate;
        public int Channels;
        public int BitDepth;
        public bool IsIEEFloatWave;
    }
}
