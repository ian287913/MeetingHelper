using System;
using System.Threading.Tasks;
using System.IO;
using Android.Media;

namespace Controller.Component
{
    public class AudioControl : IDisposable
    {
        //  Delegate function, will be called when the recorder gets any signal. (send out)
        public delegate void OnSendAudioEvent(short[] buffer, int bufferReadResult);
        public event OnSendAudioEvent OnSendAudio;
        public delegate void OnExceptionEvent(string message);
        public static event OnExceptionEvent OnException;
        //  Audio attributes
        private static int frequence = 16000;
        private static Encoding audioEncoding = Encoding.Pcm16bit;
        //  Tracker
        AudioTrack AudioTracker = null;
        public int BufferSizeTrack { get; private set; }
        //  Recorder
        AudioRecord AudioRecorder = null;
        short[] buffer;
        public int BufferSize { get; private set; }
        //  Record Audio Data
        private FileStream fs = null;
        //  Status
        public bool isRecording { get; private set; } = false;
        public bool isTracking { get; private set; } = false;
        bool disposed = false;

        /// <summary>
        /// Constructor
        /// </summary>
        public AudioControl()
        {
            RecordAsync();
        }
        /// <summary>
        /// Activate AudioRecorder
        /// </summary>
        public void StartRecord()
        {
            try
            {
                if (AudioRecorder == null)
                    CreateRecorder();
                AudioRecorder.StartRecording();
                isRecording = true;
            }
            catch (Exception e) //Create, Start 失敗
            {
                isRecording = false;
                OnException?.Invoke($"Audio Start or Record 失敗: {e.Message}");
            }
        }
        public void StopRecord()
        {
            if (AudioRecorder?.RecordingState == RecordState.Recording)
                AudioRecorder.Stop();
            isRecording = false;
        }
        /// <summary>
        /// Activate AudioTracker
        /// </summary>
        public void StartTrack()
        {
            try
            {
                if (isTracking) return;
                if (AudioTracker == null)
                    CreateTracker();
                AudioTracker.Play();
                isTracking = true;
            }
            catch (Exception e) //Create, Start 失敗
            {
                OnException?.Invoke($"AudioTrack Create 或 Play 失敗: {e.Message}");
            }
        }
        public void StopTrack()
        {
            if (AudioTracker?.PlayState == PlayState.Playing)
            {
                AudioTracker.Stop();
            }
            isTracking = false;
        }

        /// <summary>
        /// 收音async。會在Dispose()時結束週期
        /// </summary>
        async void RecordAsync()
        {
            while (!disposed)
            {
                try
                {
                    while (isRecording)
                    {
                        int bufferReadResult = await AudioRecorder.ReadAsync(buffer, 0, buffer.Length);
                        AudioWrite(buffer, bufferReadResult);
                    }
                    await Task.Delay(10);
                }
                catch (Exception e) //Read, Write 失敗
                {
                    isRecording = false;
                    OnException?.Invoke($"Audio_ReadWrite 失敗: {e.Message}");
                }
            }
        }

        /// <summary>
        /// 處理接收到的聲音(自己播放或傳給其他人)
        /// </summary>
        public void AudioWrite(short[] buffer, int bufferReadResult)
        {
            if (isTracking)  //  如果本物件正在同時收音與播音(可能發生在chairnam)
            {   //  自己播放
                AudioTracker.Write(buffer, 0, bufferReadResult);
                //  Record to file
                if (fs == null)
                    fs = CreateTempFile(Guid.NewGuid().ToString("N"));
                fs.Write(ShortsToBytes(ref buffer), 0, bufferReadResult * 2);
            }
            else
            {   //  傳給其他人
                OnSendAudio?.Invoke(buffer, bufferReadResult);
            }
        }

        public string Save()
        {
            if (fs == null)
                return ".";
            string name = fs.Name;
            fs.Close();
            fs = null;
            return name;
        }
        public void ClearEvents()
        {
            OnException = null;
            OnSendAudio = null;
        }
        //  Dispose
        protected virtual void Dispose(bool disposing)
        {
            disposed = true;
            isRecording = false;
            isTracking = false;
            fs?.Dispose();
            AudioRecorder?.Dispose();
            AudioTracker?.Dispose();
            //  terminate all async
            //  delete all audio
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        private FileStream CreateTempFile(string fileName)
        {
            string filePath = Android.OS.Environment.ExternalStorageDirectory.Path;
            DirectoryInfo file = new DirectoryInfo(filePath + "/MeetingRecord");
            if (!file.Exists) file.Create();
            FileInfo tempfile = new FileInfo(file.FullName + "/" + fileName);
            return tempfile.Create();
        }
        private byte[] ShortsToBytes(ref short[] shorts)
        {
            byte[] bytes = new byte[shorts.Length * 2];
            for (int i = 0; i < shorts.Length; i++)
            {
                bytes[2 * i] = (byte)(shorts[i] & 0xFF);
                bytes[2 * i + 1] = (byte)(shorts[i] >> 8);
            }
            return bytes;
        }
        private void CreateRecorder()
        {
            BufferSize = AudioRecord.GetMinBufferSize(frequence, ChannelIn.Mono, audioEncoding);
            AudioRecorder = new AudioRecord(AudioSource.Mic, frequence, ChannelIn.Mono, audioEncoding, BufferSize);
            buffer = new short[BufferSize];
        }
        private void CreateTracker()
        {
            BufferSizeTrack = AudioTrack.GetMinBufferSize(frequence, ChannelOut.Mono, audioEncoding);
            AudioTracker = new AudioTrack(Android.Media.Stream.VoiceCall, frequence, ChannelOut.Mono, audioEncoding, BufferSizeTrack, AudioTrackMode.Stream);
        }
    }
}

