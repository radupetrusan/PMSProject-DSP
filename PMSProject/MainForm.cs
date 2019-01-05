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
        private Note firstNote;
        private Note secondNote;
        private Note thirdNote;

        private static int _buffersCaptured = 0; // total number of audio buffers filled
        private static int _buffersRemaining = 0; // number of buffers which have yet to be analyzed

        private static double unanalyzed_max_sec; // maximum amount of unanalyzed audio to maintain in memory
        private static List<short> unanalyzed_values = new List<short>(); // audio data lives here waiting to be analyzed

        private static List<List<double>> spec_data; // columns are time points, rows are frequency points
        private static int spec_width = 600;
        private static int spec_height;
        private static int _pixelsPerBuffer;

        // sound card settings
        private int rate;
        private int buffer_update_hz;

        // spectrogram and FFT settings
        int fft_size;

        public MainForm()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            // sound card configuration
            rate = 44100;
            buffer_update_hz = 20;
            _pixelsPerBuffer = 10;

            // FFT/spectrogram configuration
            unanalyzed_max_sec = 2.5;
            fft_size = (int)Math.Pow(2, 13); // must be a multiple of 2
            spec_height = fft_size / 2;

            // fill spectrogram data with empty values
            spec_data = new List<List<double>>();
            List<double> data_empty = new List<double>();
            for (int i = 0; i < spec_height; i++) data_empty.Add(0);
            for (int i = 0; i < spec_width; i++) spec_data.Add(data_empty);

            // resize picturebox to accomodate data shape
            pictureBox1.Width = spec_data.Count;
            pictureBox1.Height = spec_data[0].Count;
            pictureBox1.Location = new Point(0, 0);

            // start listening
            var waveIn = new WaveIn();
            waveIn.DeviceNumber = 0;
            waveIn.DataAvailable += OnAudioBufferCaptured;
            waveIn.WaveFormat = new NAudio.Wave.WaveFormat(rate, 1);
            waveIn.BufferMilliseconds = 1000 / buffer_update_hz;
            waveIn.StartRecording();

            timer1.Enabled = true;
            timer2.Start();
        }

        /// <summary>
        /// analyze all unanalyzed data
        /// </summary>
        void Analyze_values()
        {
            if (fft_size == 0) return;
            if (unanalyzed_values.Count < fft_size) return;
            label4.Text = string.Format("Analysis: {0}", unanalyzed_values.Count);
            while (unanalyzed_values.Count >= fft_size) Analyze_chunk();
            label4.Text = string.Format("Analysis: up to date");
        }

        /// <summary>
        /// break-off the first chunk of unanalyzed_values and analyze it
        /// </summary>
        void Analyze_chunk()
        {
            // fill data with FFT info
            short[] data = new short[fft_size];
            data = unanalyzed_values.GetRange(0, fft_size).ToArray();

            // remove the left-most (oldest) column of data
            spec_data.RemoveAt(0);

            // insert new data to the right-most (newest) position
            List<double> new_data = new List<double>();


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

            //double first = 0;
            //var firstIndex = 0;

            //double second = 0;
            //var secondIndex = 0;

            //double third = 0;
            //var thirdIndex = 0;

            // fill that new data list with fft values
            for (int i = 0; i < spec_data[spec_data.Count - 1].Count; i++)
            {
                // should this be sqrt(X^2+Y^2)?
                double val;
                //val = (double)fft_buffer[i].X + (double)fft_buffer[i].Y;
                val = Math.Sqrt(Math.Pow(fft_buffer[i].X, 2) + Math.Pow(fft_buffer[i].Y, 2));
                val = Math.Abs(val);
                if (checkBox1.Checked) val = Math.Log(val);

                //if (val > first)
                //{
                //    third = second;
                //    thirdIndex = secondIndex;

                //    second = first;
                //    secondIndex = firstIndex;

                //    first = val;
                //    firstIndex = i;
                //}
                //else
                //{
                //    if (val > second)
                //    {
                //        third = second;
                //        thirdIndex = secondIndex;

                //        second = val;
                //        secondIndex = i;
                //    }
                //    else
                //    {
                //        if (val > third)
                //        {
                //            third = val;
                //            thirdIndex = i;
                //        }
                //    }
                //}

                new_data.Add(val);
            }

            var filterRange = 20;

            var myData = new_data.ToList();
            myData.RemoveRange(300, new_data.Count - 300);

            var firstIndex = myData.MaxIndex();
            
            ArrayUtils<double>.RemovePeakNeighbours(myData, firstIndex, filterRange);

            var secondIndex = myData.MaxIndex();
            var myOtherData = myData.ToList();
            ArrayUtils<double>.RemovePeakNeighbours(myOtherData, secondIndex, filterRange);
            var thirdIndex = myOtherData.MaxIndex();

            var firstFreq = firstIndex * rate / fft_size;
            var secondFreq = secondIndex * rate / fft_size;
            var thirdFreq = thirdIndex * rate / fft_size;

            if (new_data[firstIndex] < 0.5)
            {
                firstFreq = secondFreq = thirdFreq = -1;
            }

            if (new_data[secondIndex] < 0.25)
            {
                secondFreq = thirdFreq = -1;
            }

            if (new_data[thirdIndex] < 0.125)
            {
                thirdFreq = -1;
            }

            Invoke(new MethodInvoker(delegate ()
            {
                firstNote = NotesUtils.ComputeNote(firstFreq);
                secondNote = NotesUtils.ComputeNote(secondFreq);
                thirdNote = NotesUtils.ComputeNote(thirdFreq);

                var previousNote = NotesUtils.GetPrevoiusNote(firstNote);
                var nextNote = NotesUtils.GetNextNote(firstNote);

                actualNoteLabel.Text = firstNote.FullNoteName;
                previusNoteLabel.Text = previousNote.FullNoteName;
                nextNoteLabel.Text = nextNote.FullNoteName;
                firstTrackBar.Value = 50 + firstNote.DifferenceFromReference;

                if (Math.Abs(firstNote.DifferenceFromReference) > 10)
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

            //new_data.Reverse();
            spec_data.Insert(spec_data.Count, new_data); // replaces, doesn't append!

            // remove a certain amount of unanalyzed data
            unanalyzed_values.RemoveRange(0, fft_size / _pixelsPerBuffer);

        }

        void UpdateBitmapWithData()
        {
            // create a bitmap we will work with
            Bitmap bitmap = new Bitmap(spec_data.Count, spec_data[0].Count, PixelFormat.Format8bppIndexed);

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
            for (int col = 0; col < spec_data.Count; col++)
            {

                // I selected this manually to yield a number that "looked good"
                double scaleFactor;
                scaleFactor = (double)numericUpDown1.Value;

                for (int row = 0; row < spec_data[col].Count; row++)
                {
                    int bytePosition = row * bitmapData.Stride + col;
                    double pixelVal = spec_data[col][row] * scaleFactor;
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

        /// <summary>
        /// runs every time the recording buffer fills-up with new audio data.
        /// </summary>
        void OnAudioBufferCaptured(object sender, WaveInEventArgs args)
        {
            _buffersCaptured += 1;
            _buffersRemaining += 1;

            // interpret as 16 bit audio, so each two bytes become one value
            short[] values = new short[args.Buffer.Length / 2];
            for (int i = 0; i < args.BytesRecorded; i += 2)
            {
                values[i / 2] = (short)((args.Buffer[i + 1] << 8) | args.Buffer[i + 0]);
            }

            // add these values to the growing list, but ensure it doesn't get too big
            unanalyzed_values.AddRange(values);

            int unanalyzed_max_count = (int)unanalyzed_max_sec * rate;

            if (unanalyzed_values.Count > unanalyzed_max_count)
            {
                unanalyzed_values.RemoveRange(0, unanalyzed_values.Count - unanalyzed_max_count);
            }

            label1.Text = string.Format("Buffers captured: {0}", _buffersCaptured);
            label2.Text = string.Format("Buffer size: {0}", values.Length);
            label3.Text = string.Format("Unanalyzed values: {0}", unanalyzed_values.Count);
        }

        /// <summary>
        /// every so often clear-out the unanalyzed audio buffer and update the spectrograph
        /// </summary>
        private void timer1_Tick(object sender, EventArgs e)
        {
            Analyze_values();
            UpdateBitmapWithData();
        }

        private void panel1_Paint(object sender, PaintEventArgs e)
        {
            PanelUtils.InitializeGraphics(e.Graphics);
            PanelUtils.InitializePanel(panel1, null);

            PanelUtils.DrawNote(firstNote, panel1);
            PanelUtils.DrawNote(secondNote, panel1);
            PanelUtils.DrawNote(thirdNote, panel1);
        }

        private void timer2_Tick(object sender, EventArgs e)
        {
            this.Refresh();
        }
    }
}
