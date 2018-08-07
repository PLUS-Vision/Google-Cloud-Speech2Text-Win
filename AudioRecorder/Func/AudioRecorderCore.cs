using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace AudioRecorder.Func
{
    public class AudioRecorderCore:IDisposable
    {
        const Int32 MaxRecordingTime = 60 * 60 * 24;
        RecordingState Status = RecordingState.Stopped;
        //comboBoxSampleRate
        readonly Int32[] SampleRate = new Int32[] { 8000, 16000, 22050, 32000, 44100, 48000 };
        //comboBoxChannels
        readonly String[] Channels = new[] { "Mono", "Stereo" };

        WaveInCapabilities[] WaveInDevice;

        private IWaveIn captureDevice;
        private WaveFileWriter writer;
        private readonly string outputFolder;

        public AudioRecorderCore()
        {
            LoadWaveInDevices();

            outputFolder = Path.Combine(Path.GetTempPath(), "Ai-Recorder-Service");
            Directory.CreateDirectory(outputFolder);

        }

        public void Dispose()
        {
            Cleanup();
        }

        protected void StopRecording()
        {
            Debug.WriteLine("StopRecording");
            captureDevice?.StopRecording();
        }
        protected void StartRecording()
        {
            Cleanup(); // WaveIn is still unreliable in some circumstances to being reused

            if (captureDevice == null)
            {
                captureDevice = CreateWaveInDevice();
            }
            //// Forcibly turn on the microphone (some programs (Skype) turn it off).
            //var device = (MMDevice)comboWasapiDevices.SelectedItem;
            //device.AudioEndpointVolume.Mute = false;

            OutputFilename = GetFileName();
            writer = new WaveFileWriter(Path.Combine(outputFolder, OutputFilename), captureDevice.WaveFormat);
            captureDevice.StartRecording();
            SetControlStates(true);
        }
        #region Public Properties
        protected string OutputFilename
        {
            get;
            private set;
        }
        public int SecondsRecorded
        {
            private set;
            get;
        }
        public int SelectedWaveInDevice
        {
            set;
            get;
        }
        public int SelectedSampleRate
        {
            set;
            get;
        }
        public int SelectedChannel
        {
            set;
            get;
        }
        public bool EventCallbackEnabled
        {
            set;
            private get;
        }
        #endregion

        #region Private Functions
        
        private void LoadWaveInDevices()
        {
            WaveInDevice = Enumerable.Range(
                -1, 
                WaveIn.DeviceCount + 1).Select
                (
                n => WaveIn.GetCapabilities(n)
                ).ToArray();
        }

        private string GetFileName()
        {
            var deviceName = captureDevice.GetType().Name;
            var sampleRate = $"{captureDevice.WaveFormat.SampleRate / 1000}kHz";
            var channels = captureDevice.WaveFormat.Channels == 1 ? "mono" : "stereo";

            return $"{deviceName} {sampleRate} {channels} {DateTime.Now:yyy-MM-dd HH-mm-ss}.wav";
        }

        private IWaveIn CreateWaveInDevice()
        {
            IWaveIn newWaveIn;

            var deviceNumber = SelectedWaveInDevice - 1;
            if (EventCallbackEnabled)
            {
                newWaveIn = new WaveInEvent() { DeviceNumber = deviceNumber };
            }
            else
            {
                newWaveIn = new WaveIn() { DeviceNumber = deviceNumber };
            }
            var sampleRate = SelectedSampleRate;
            var channels = SelectedChannel + 1;
            newWaveIn.WaveFormat = new WaveFormat(sampleRate, channels);

            newWaveIn.DataAvailable += OnDataAvailable;
            newWaveIn.RecordingStopped += OnRecordingStopped;
            return newWaveIn;
        }

        void OnRecordingStopped(object sender, StoppedEventArgs e)
        {
            FinalizeWaveFile();
            SecondsRecorded = 0;
            if (e.Exception != null)
            {
                MessageBox.Show(String.Format("A problem was encountered during recording {0}",
                                              e.Exception.Message));
            }

            SetControlStates(false);
        }

        private void Cleanup()
        {
            if (captureDevice != null)
            {
                captureDevice.Dispose();
                captureDevice = null;
            }
            FinalizeWaveFile();
        }
        private void FinalizeWaveFile()
        {
            writer?.Dispose();
            writer = null;
        }
        void OnDataAvailable(object sender, WaveInEventArgs e)
        {
            //Debug.WriteLine("Flushing Data Available");
            writer.Write(e.Buffer, 0, e.BytesRecorded);
            SecondsRecorded = (int)(writer.Length / writer.WaveFormat.AverageBytesPerSecond);
            if (SecondsRecorded >= MaxRecordingTime)
            {
                StopRecording();
            }
        }        
        private void SetControlStates(bool isRecording)
        {
            Status = isRecording ? RecordingState.Recording : RecordingState.Stopped;
        }
        private void OnOpenFolderClick(object sender, EventArgs e)
        {
            Process.Start(outputFolder);
        }
        #endregion

    }
}
