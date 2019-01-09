using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

using NAudio.Wave; // For sound card access
using NAudio.Dsp; // For FastFourierTransform
using System.Drawing.Imaging; // For ImageLockMode
using System.Runtime.InteropServices; // For Marshal
using PMSProject.Utils;
using PMSProject.Extensions;
using PMSProject.Models;

namespace PMSProject
{
    public partial class MainForm : Form
    {
        private Note _firstNote;
        private Note _secondNote;
        private Note _thirdNote;

        private static int _buffersCaptured = 0; // total number of audio buffers filled

        private static double unanalyzed_max_sec; // maximum amount of unanalyzed audio to maintain in memory
        private static List<short> unanalyzed_values = new List<short>(); // audio data lives here waiting to be analyzed

        private static List<List<double>> spectrogram_data = new List<List<double>>(); // columns are time points, rows are frequency points
        private static int SPECTROGRAM_WIDTH = 600;
        private static int SPECTROGRAM_HEIGHT;
        private static int _pixelsPerBuffer = 10;

        // sound card settings
        private static int SAMPLE_RATE = 44100;
        private int buffer_update_hz = 20;

        // spectrogram and FFT settings
        int fft_size;

        public MainForm()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            // FFT/spectrogram configuration
            unanalyzed_max_sec = 2.5;
            fft_size = (int)Math.Pow(2, 13); // must be a multiple of 2 (8192)
            SPECTROGRAM_HEIGHT = fft_size / 2;

            // fill spectrogram data with empty values
            spectrogram_data = new List<List<double>>();
            List<double> data_empty = new List<double>();
            for (int i = 0; i < SPECTROGRAM_HEIGHT; i++) data_empty.Add(0);
            for (int i = 0; i < SPECTROGRAM_WIDTH; i++) spectrogram_data.Add(data_empty);

            // resize picturebox to accomodate data shape
            pictureBox1.Width = spectrogram_data.Count;
            pictureBox1.Height = spectrogram_data[0].Count;
            pictureBox1.Location = new Point(0, 0);

            StartAudioListening();

            timer1.Start();
            timer2.Start();
        }

        private void StartAudioListening()
        {
            var waveIn = new WaveIn();
            waveIn.DeviceNumber = 0;
            waveIn.DataAvailable += OnAudioBufferCaptured;
            waveIn.WaveFormat = new NAudio.Wave.WaveFormat(SAMPLE_RATE, 1);
            waveIn.BufferMilliseconds = 1000 / buffer_update_hz;
            waveIn.StartRecording();
        }

        void AnalyzeValues()
        {
            if (fft_size == 0) return;
            if (unanalyzed_values.Count < fft_size) return;
            label4.Text = string.Format("Analysis: {0}", unanalyzed_values.Count);
            while (unanalyzed_values.Count >= fft_size) AnalyzeChunk();
            label4.Text = string.Format("Analysis: up to date");
        }

        void AnalyzeChunk()
        {
            // fill data with FFT info
            var data = new short[fft_size];
            data = unanalyzed_values.GetRange(0, fft_size).ToArray();

            // remove the left-most (oldest) column of data
            spectrogram_data.RemoveAt(0);

            // insert new data to the right-most (newest) position
            var newData = new List<double>();

            // prepare the complex data which will be FFT'd
            Complex[] fft_buffer = new Complex[fft_size];
            for (int i = 0; i < fft_size; i++)
            {
                //fft_buffer[i].X = (float)unanalyzed_values[i]; // no window
                fft_buffer[i].X = (float)(unanalyzed_values[i] * FastFourierTransform.HammingWindow(i, fft_size));
                fft_buffer[i].Y = 0;
            }

            // perform the FFT
            FastFourierTransform.FFT(true, (int)Math.Log(fft_size, 2.0), fft_buffer);

            // fill that new data list with fft values
            for (int i = 0; i < spectrogram_data[spectrogram_data.Count - 1].Count; i++)
            {
                // sqrt(X^2+Y^2)?
                //var val = (double)fft_buffer[i].X + (double)fft_buffer[i].Y;
                var val = Math.Sqrt(Math.Pow(fft_buffer[i].X, 2) + Math.Pow(fft_buffer[i].Y, 2));
                //val = Math.Abs(val);
                if (checkBox1.Checked) val = Math.Log(val);

                newData.Add(val);
            }

            var filterRange = 10;


            var myData = newData.ToList();
            myData.RemoveRange(300, newData.Count - 300);

            var firstIndex = myData.MaxIndex();

            ArrayUtils.RemovePeakNeighbours(myData, firstIndex, filterRange);
            var secondIndex = myData.MaxIndex();

            ArrayUtils.RemovePeakNeighbours(myData, secondIndex, filterRange);
            var thirdIndex = myData.MaxIndex();

            var firstFreq = (firstIndex + 1) * SAMPLE_RATE / fft_size;
            var secondFreq = (secondIndex + 1) * SAMPLE_RATE / fft_size;
            var thirdFreq = (thirdIndex + 1) * SAMPLE_RATE / fft_size;

            if (newData[firstIndex] < 0.5)
            {
                firstFreq = secondFreq = thirdFreq = -1;
            }

            if (newData[secondIndex] < 0.25)
            {
                secondFreq = thirdFreq = -1;
            }

            if (newData[thirdIndex] < 0.125)
            {
                thirdFreq = -1;
            }

            Invoke(new MethodInvoker(delegate ()
            {
                _firstNote = NotesUtils.ComputeNote(firstFreq);
                _secondNote = NotesUtils.ComputeNote(secondFreq);
                _thirdNote = NotesUtils.ComputeNote(thirdFreq);

                var previousNote = NotesUtils.GetPrevoiusNote(_firstNote);
                var nextNote = NotesUtils.GetNextNote(_firstNote);

                actualNoteLabel.Text = _firstNote.FullNoteName;
                previusNoteLabel.Text = previousNote.FullNoteName;
                nextNoteLabel.Text = nextNote.FullNoteName;
                firstTrackBar.Value = 50 + _firstNote.DifferenceFromReference;

                if (Math.Abs(_firstNote.DifferenceFromReference) > 10)
                {
                    firstTrackBar.BackColor = Color.Red;
                    actualNoteLabel.ForeColor = Color.Red;
                }
                else
                {
                    firstTrackBar.BackColor = Color.Green;
                    actualNoteLabel.ForeColor = Color.Green;
                }
                

                firstFreqLabel.Text = "First freq: " + firstFreq.ToString() + " Hz";
                secondFreqLabel.Text = "Second freq: " + secondFreq.ToString() + " Hz";
                thirdFreqLabel.Text = "Thrid freq: " + thirdFreq.ToString() + " Hz";
            }));

            //newData.Reverse();
            spectrogram_data.Insert(spectrogram_data.Count, newData);

            // remove a certain amount of unanalyzed data
            unanalyzed_values.RemoveRange(0, fft_size / _pixelsPerBuffer);

        }

        void UpdateBitmapWithData()
        {
            // create a bitmap we will work with
            Bitmap bitmap = new Bitmap(spectrogram_data.Count, spectrogram_data[0].Count, PixelFormat.Format8bppIndexed);

            // modify the indexed palette to make it grayscale
            ColorPalette pal = bitmap.Palette;
            for (int i = 0; i < 256; i++)
                pal.Entries[i] = Color.FromArgb(255, i, i, i);
            bitmap.Palette = pal;

            // prepare to access data via the bitmapdata object
            BitmapData bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                                                    ImageLockMode.ReadOnly, bitmap.PixelFormat);

            // create a byte array to reflect each pixel in the image
            byte[] pixels = new byte[bitmapData.Stride * bitmap.Height];

            // fill pixel array with data
            for (int col = 0; col < spectrogram_data.Count; col++)
            {

                // I selected this manually to yield a number that "looked good"
                double scaleFactor;
                scaleFactor = (double)numericUpDown1.Value;

                for (int row = 0; row < spectrogram_data[col].Count; row++)
                {
                    int bytePosition = row * bitmapData.Stride + col;
                    double pixelVal = spectrogram_data[col][row] * scaleFactor;
                    pixelVal = Math.Max(0, pixelVal);
                    pixelVal = Math.Min(255, pixelVal);
                    pixels[bytePosition] = (byte)(pixelVal);
                }
            }

            // turn the byte array back into a bitmap
            Marshal.Copy(pixels, 0, bitmapData.Scan0, pixels.Length);
            bitmap.UnlockBits(bitmapData);

            // apply the bitmap to the picturebox
            pictureBox1.Image = bitmap;
        }

        void OnAudioBufferCaptured(object sender, WaveInEventArgs args)
        {
            _buffersCaptured += 1;

            // interpret as 16 bit audio, so each two bytes become one value
            short[] values = new short[args.Buffer.Length / 2];
            for (int i = 0; i < args.BytesRecorded; i += 2)
            {
                values[i / 2] = (short)((args.Buffer[i + 1] << 8) | args.Buffer[i + 0]);
            }

            // add these values to the growing list, but ensure it doesn't get too big
            unanalyzed_values.AddRange(values);

            int unanalyzed_max_count = (int)unanalyzed_max_sec * SAMPLE_RATE;

            if (unanalyzed_values.Count > unanalyzed_max_count)
            {
                unanalyzed_values.RemoveRange(0, unanalyzed_values.Count - unanalyzed_max_count);
            }

            label1.Text = string.Format("Buffers captured: {0}", _buffersCaptured);
            label2.Text = string.Format("Buffer size: {0}", values.Length);
            label3.Text = string.Format("Unanalyzed values: {0}", unanalyzed_values.Count);
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            AnalyzeValues();
            UpdateBitmapWithData();
        }

        private void panel1_Paint(object sender, PaintEventArgs e)
        {
            PanelUtils.InitializeGraphics(e.Graphics);
            PanelUtils.InitializePanel(panel1, null);

            PanelUtils.DrawNote(_firstNote, panel1);
            PanelUtils.DrawNote(_secondNote, panel1);
            PanelUtils.DrawNote(_thirdNote, panel1);
        }

        private void timer2_Tick(object sender, EventArgs e)
        {
            this.Refresh();
        }
    }
}
