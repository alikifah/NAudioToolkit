
namespace NAudioToolkit
{
    public interface IAudioRecorder
    {
        event Action<byte[]> SamplesReady;
        void Start();
        void Stop();
    }

    public interface IAudioPlayer
    {
        void AddAudioData(byte[] audioBytes);
        void AddAudioData(short[] audioBytes);
        void AddAudioData(float[] audioBytes);
        void Play();
        void Stop();
        void Pause();
    }
}
