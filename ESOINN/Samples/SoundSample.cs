using Main.Network;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Shapes;

namespace Main.Samples
{
    public class SoundSample : SampleBase
    {
        public override Rectangle Rectangle
        {
            get { return new Rectangle(); }
        }

        int samplesCount;
        public override int SamplesCount
        {
            get { return samplesCount + 1; }
        }

        private IWaveIn recorder;
        private List<double[]> buffer;

        public int Time { get; set; }
        public double NoiseThreshold { get; set; }


        public SoundSample(bool input)
        {
            if (input)
            {
                this.recorder = new WaveIn();
                (this.recorder as WaveIn).BufferMilliseconds = 500;
                this.recorder.WaveFormat = new WaveFormat(8000, 8, 1);
            }
            else
                this.recorder = new WasapiLoopbackCapture();
            this.recorder.DataAvailable += recorderOnDataAvailable;
            this.recorder.StartRecording();

            buffer = new List<double[]>();

            int age = 100;
            Model = new ESOINN(100, age, age + age / 2 );
            ReduceNoise = true;
            Winners = new List<Vertex>();

            this.Time = 10;
            this.NoiseThreshold = 0.01;
        }

        public override void Dispose()
        {
            recorder.StopRecording();
        }


        public override bool Process(int sample, bool learn = true)
        {
            if (buffer.Count == 0 || buffer[0] == null || buffer[0].Length != Model.Dim)
            {
                if (buffer.Count != 0)
                    buffer.RemoveAt(0);
                return false;
            }

            if (buffer[0] != null)
            {
                var winner = learn ? Model.Process(buffer[0]) : Model.GetBestMatch(buffer[0]);
                Winners.Add(winner);
            }
            buffer.RemoveAt(0);
            return true;
        }

        public override Point GetVertexPosition(Vertex vertex)
        {
            Point result = new Point();
            for (int i = 0; i < vertex.Weight.Length; i++)
            {
                if (i % 2 == 0)
                    result.X += vertex.Weight[i];
                else
                    result.Y += vertex.Weight[i];
            }
            //result.X /= vertex.Weight.Length / 2;
            //result.Y /= vertex.Weight.Length - vertex.Weight.Length / 2;
            return result;
        }


        private List<double> data = new List<double>();
        private int noiseCounter = 0;
        private void recorderOnDataAvailable(object sender, WaveInEventArgs e)
        {
            var format = recorder.WaveFormat;
            int rate = (format.BitsPerSample / 8) * format.Channels;
            for (int i = 0; i < e.BytesRecorded / rate; i++)
            {
                if (format.Encoding == WaveFormatEncoding.IeeeFloat)
                    data.Add(BitConverter.ToSingle(e.Buffer, i * rate));
                else
                    data.Add(ConvertValue(format.BitsPerSample, e.Buffer, i * rate));

                if (ReduceNoise && Math.Abs(data[data.Count - 1]) < this.NoiseThreshold)
                {
                    noiseCounter++;
                    if (noiseCounter >= format.SampleRate / (1000 / this.Time))
                        data.Clear();

                    if (noiseCounter >= (format.SampleRate / (1000 / this.Time)) * 2)
                        if (Winners.Count > 0 && Winners[Winners.Count - 1] != null && this.buffer.Count == 0)
                            Winners.Add(null);
                }
                else
                    noiseCounter = 0;

                if (data.Count == format.SampleRate / (1000 / this.Time)) // samples for 'Time' ms
                {
                    double[] realIn = new double[DSP.FourierTransform.NextPowerOfTwo((uint)data.Count)];
                    data.CopyTo(realIn);
                    double[] realOut = new double[realIn.Length];
                    if (true) // Fourier or not?
                        DSP.FourierTransform.Compute((uint)realIn.Length, ref realIn, new double[realIn.Length], realOut, new double[realIn.Length], false);
                    else
                        realOut = data.ToArray();

                    double[] buff = new double[Model.Dim];
                    for (int j = 0; j < realOut.Length; j++)
                    {
                        int idx = (int)(j / ((double)realOut.Length / Model.Dim));
                        buff[idx] += realOut[j];
                    }
                    this.buffer.Add(buff);
                    samplesCount++;
                    data.Clear();
                }
            }
        }
        
        public static double ConvertValue(int rate, byte[] array, int startIndex)
        {
            double result = 0;
            if (rate == 8)
                result = (double)(array[startIndex] - 128) / 128;
            else if (rate == 16)
                result = (double)BitConverter.ToInt16(array, startIndex) / Int16.MaxValue;
            else if (rate == 32)
                result = (double)BitConverter.ToInt32(array, startIndex) / Int32.MaxValue;
            else if (rate == 64)
                result = (double)BitConverter.ToInt64(array, startIndex) / Int64.MaxValue;
            return result;
        }
    }
}
