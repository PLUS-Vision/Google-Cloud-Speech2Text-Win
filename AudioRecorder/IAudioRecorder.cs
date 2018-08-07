using NAudio.Wave;
using System;

namespace AudioRecorder
{
    public interface IAudioRecorder
    {
        void BeginRecording();
        void StopRecording();

        RecordingState RecordingState { get; }
        SampleAggregator SampleAggregator { get; }
        event EventHandler Stopped;
        WaveFormat RecordingFormat { get; set; }
        TimeSpan RecordedTime { get; }
    }
}
