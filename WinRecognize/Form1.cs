
using NAudio.Wave;
//Credit goes to https://github.com/naudio/NAudio
//
//Steve Cox - 10/13/17 - All audio code comes several NAudio projects. I mashed up just the code I needed for this demo
//Steve Cox - 12/23/17 - Added timer and peak audio detector code for a simple voice activated effect


using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using Google.Apis.Auth.OAuth2;
using Google.Cloud.Language.V1;
using Google.Cloud.Speech.V1;

using Grpc.Auth;
using Google.Protobuf.Collections;
using System.Threading;
using NAudio.Mixer;
using AudioRecorder;
//using Google.Apis.CloudNaturalLanguageAPI.v1beta1.Data;

//NOTE: You will have to goto "Tools->Nuget Package Manager->Packet Manager Console" and copy paste, then run the below commands
//Install-Package Google.Cloud.Speech.V1 -Version 1.0.0-beta08 -Pre
//Install-Package Google.Cloud.Language.V1 -Pre

namespace WinRecognize
{
    public partial class Form1 : Form
    {
        private IAudioRecorder audioRecorder = new AudioRecorder.Func.AudioRecorder();

        private Boolean monitoring = false;
        
        //private RecognitionConfig oneShotConfig;
        //private SpeechClient speech = SpeechClient.Create();
        //private SpeechClient.StreamingRecognizeStream streamingCall;
        //private StreamingRecognizeRequest streamingRequest;

        private BufferedWaveProvider waveBuffer;
        
        // Read from the microphone and stream to API.
        //private WaveInEvent waveIn = new NAudio.Wave.WaveInEvent();
        
       
        public Form1()
        {
            InitializeComponent();

            if (NAudio.Wave.WaveIn.DeviceCount < 1)
            {
                MessageBox.Show("No microphone! ... exiting");
                return;
            }
        }
                
        /// <summary>
        /// Wave in recording task gets called when we think we have enough audio to send to googles
        /// </summary>
        /// <param name="seconds"></param>
        /// <returns></returns>
        private async Task<object> StreamBufferToGooglesAsync()
        {
            //I don't like having to re-create these everytime, but breaking the
            //code out is for another refactoring.
            var speech = SpeechClient.Create();
            var streamingCall = speech.StreamingRecognize();
            
            // Write the initial request with the config.
            //Again, this is googles code example, I tried unrolling this stuff
            //and the google api stopped working, so stays like this for now
            await streamingCall.WriteAsync(new StreamingRecognizeRequest()
            {
                StreamingConfig = new StreamingRecognitionConfig()
                {
                    Config = new RecognitionConfig()
                    {
                        Encoding = RecognitionConfig.Types.AudioEncoding.Linear16,
                        SampleRateHertz = 16000,
                        LanguageCode = "ja-JP",
                    },

                    //Note: play with this value
                    // InterimResults = true,  // this needs to be true for real time
                    SingleUtterance = true,
                }
            });

            

            //Get what ever data we have in our internal wave buffer and put into
            //byte array for googles
            byte[] buffer = new byte[waveBuffer.BufferLength];
            int offset = 0;
            int count = waveBuffer.BufferLength;

            //Gulp ... yummy bytes ....
            waveBuffer.Read(buffer, offset, count);

            try
             {
                //Sending to Googles .... finally
                 streamingCall.WriteAsync(new StreamingRecognizeRequest()
                 {
                     AudioContent = Google.Protobuf.ByteString.CopyFrom(buffer, 0, count)
                 }).Wait();
             }
             catch (Exception wtf)
             {
                 string wtfMessage = wtf.Message;
             }

            //Again, this is googles code example below, I tried unrolling this stuff
            //and the google api stopped working, so stays like this for now

            //Print responses as they arrive. Need to move this into a method for cleanslyness
            Task printResponses = Task.Run(async () =>
            {
                string saidWhat = "";
                string lastSaidWhat = "";
                while (await streamingCall.ResponseStream.MoveNext(default(CancellationToken)))
                {
                    foreach (var result in streamingCall.ResponseStream.Current.Results)
                    {
                        foreach (var alternative in result.Alternatives)
                        {
                            saidWhat = alternative.Transcript;
                            if (lastSaidWhat != saidWhat)
                            {
                                Console.WriteLine(saidWhat);
                                lastSaidWhat = saidWhat;
                                //Need to call this on UI thread ....
                                textBox1.Invoke((MethodInvoker)delegate { textBox1.AppendText(textBox1.Text + saidWhat + " \r\n"); });
                            }

                        }  // end for

                    } // end for


                }
            });

            //Clear our internal wave buffer
            waveBuffer.ClearBuffer();

            //Tell googles we are done for now
            await streamingCall.WriteCompleteAsync();
            
            return 0;
        }
        

        /// <summary>
        /// Starts the voice activated audio recording
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button3_Click(object sender, EventArgs e)
        {
            if (NAudio.Wave.WaveIn.DeviceCount > 0)
            {
                if(monitoring == false)
                {
                    monitoring = true;
                    //Begin
                    audioRecorder.BeginRecording();

                    button3.Text = "Record Stop";
                }
                else
                {
                    monitoring = false;
                    audioRecorder.StopRecording();

                    button3.Text = "Record Start";
                }
            }
            
        }
    }
}
