using NAudio.Mixer;
using NAudio.Wave;
using System;
using System.Linq;

namespace AudioRecorder.Func
{
    public class AudioRecorder : AudioRecorderCore, IAudioRecorder
    {
        const int DefaultDevice = 1;
        private SampleAggregator sampleAggregator;
        RecordingState recordingState;
        WaveFormat recordingFormat;

        public event EventHandler Stopped = delegate { };

        public AudioRecorder()
            : this(sampleRate: 16000, channels: 1, device: 1)
        {
            sampleAggregator = new SampleAggregator();

            RecordingFormat = new WaveFormat(SelectedSampleRate, SelectedChannel + 1);
        }
        private AudioRecorder(int sampleRate, int channels, int device)
        {
            SelectedSampleRate = sampleRate;
            SelectedChannel = channels - 1;
            SelectedWaveInDevice = device;
        }

        public WaveFormat RecordingFormat
        {
            get
            {
                return new WaveFormat(SelectedSampleRate, SelectedChannel + 1);
            }
            set
            {
                recordingFormat = value;

                SelectedSampleRate = value.SampleRate;
                SelectedChannel = value.Channels - 1;

                sampleAggregator.NotificationCount = SelectedSampleRate / 10;
            }
        }

        public void BeginRecording()
        {
            if (recordingState != RecordingState.Stopped)
            {
                throw new InvalidOperationException("Can't begin monitoring while we are in this state: " + recordingState.ToString());
            }

            base.StartRecording();
            recordingState = RecordingState.Recording;
        }

        public new void StopRecording()
        {
            if (recordingState == RecordingState.Recording)
            {
                recordingState = RecordingState.RequestedStop;
                base.StopRecording();
            }
        }
        public SampleAggregator SampleAggregator
        {
            get
            {
                return sampleAggregator;
            }
        }

        public RecordingState RecordingState
        {
            get
            {
                return recordingState;
            }
        }

        public TimeSpan RecordedTime
        {
            get
            {
                return TimeSpan.FromSeconds(
                    base.SecondsRecorded
                    );
            }
        }

        public String RecordedFile
        {
            get
            {
                return base.OutputFilename;
            }
        }

        void OnDataAvailable(object sender, WaveInEventArgs e)
        {
            byte[] buffer = e.Buffer;
            int bytesRecorded = e.BytesRecorded;
            //WriteToFile(buffer, bytesRecorded);

            for (int index = 0; index < e.BytesRecorded; index += 2)
            {
                short sample = (short)((buffer[index + 1] << 8) |
                                        buffer[index + 0]);
                float sample32 = sample / 32768f;
                sampleAggregator.Add(sample32);
            }
        }

    }
}
