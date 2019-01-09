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
        private Note _firstNote = new Note();
        private Note _secondNote = new Note();
        private Note _thirdNote = new Note();

        private Note _newFirstNote = new Note();
        private Note _newSecondNote = new Note();
        private Note _newThirdNote = new Note();

        private Dictionary<string, PictureBox> signs;

        private static int _buffersCaptured = 0; // total number of audio buffers filled

        private static double unanalyzed_max_sec = 2.5; // maximum amount of unanalyzed audio to maintain in memory
        private static List<short> unanalyzed_values = new List<short>(); // audio data lives here waiting to be analyzed

        private static List<List<double>> spectrogram_data = new List<List<double>>(); // columns are time points, rows are frequency points
        private static int SPECTROGRAM_WIDTH = 600;
        private static int SPECTROGRAM_HEIGHT;
        private static int _pixelsPerBuffer = 10;

        private static bool showSigns = false;

        // sound card settings
        private static int SAMPLE_RATE = 44100;
        private int BUFFER_UPDATE_FREQUENCY = 20; // 20 Hz

        // spectrogram and FFT settings
        int FFT_SIZE = (int)Math.Pow(2, 13); // must be a multiple of 2 (8192);

        public MainForm()
        {
            InitializeComponent();
            signs = GetSharpsFlatsPicBoxes();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            // FFT/spectrogram configuration
            SPECTROGRAM_HEIGHT = FFT_SIZE / 2;

            // fill spectrogram data with empty values
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
            waveIn.BufferMilliseconds = 1000 / BUFFER_UPDATE_FREQUENCY;
            waveIn.StartRecording();
        }

        void AnalyzeValues()
        {
            if (FFT_SIZE == 0 || unanalyzed_values.Count < FFT_SIZE)
                return;

            //if (unanalyzed_values.Count < FFT_SIZE) return;
            //label4.Text = string.Format("Analysis: {0}", unanalyzed_values.Count);

            while (unanalyzed_values.Count >= FFT_SIZE)
                AnalyzeChunk();

            //label4.Text = string.Format("Analysis: up to date");
        }

        void AnalyzeChunk()
        {
            // fill data with FFT info
            var data = new short[FFT_SIZE];
            data = unanalyzed_values.GetRange(0, FFT_SIZE).ToArray();

            // remove the left-most (oldest) column of data
            spectrogram_data.RemoveAt(0);

            // insert new data to the right-most (newest) position
            var newData = new List<double>();

            // prepare the complex data which will be FFT'd
            Complex[] fft_buffer = new Complex[FFT_SIZE];
            for (int i = 0; i < FFT_SIZE; i++)
            {
                //fft_buffer[i].X = (float)unanalyzed_values[i]; // no window
                fft_buffer[i].X = (float)(unanalyzed_values[i] * FastFourierTransform.HammingWindow(i, FFT_SIZE)); // Hamming Window
                fft_buffer[i].Y = 0;
            }

            // perform the FFT
            FastFourierTransform.FFT(true, (int)Math.Log(FFT_SIZE, 2.0), fft_buffer);

            // fill that new data list with fft values
            for (int i = 0; i < spectrogram_data[spectrogram_data.Count - 1].Count; i++)
            {
                // sqrt(X^2+Y^2)
                //var val = (double)fft_buffer[i].X + (double)fft_buffer[i].Y;
                var val = Math.Sqrt(Math.Pow(fft_buffer[i].X, 2) + Math.Pow(fft_buffer[i].Y, 2));
                //val = Math.Abs(val);
                if (checkBox1.Checked) val = Math.Log(val);

                newData.Add(val);
            }

            ProccessNotes(newData);

            //newData.Reverse();
            spectrogram_data.Insert(spectrogram_data.Count, newData);

            // remove a certain amount of unanalyzed data
            unanalyzed_values.RemoveRange(0, FFT_SIZE / _pixelsPerBuffer);
        }

        void ProccessNotes(List<double> data)
        {
            var filterRange = 10;

            var myData = data.ToList();
            data.RemoveRange(300, data.Count - 300);

            var firstIndex = myData.MaxIndex();

            ArrayUtils.RemovePeakNeighbours(myData, firstIndex, filterRange);
            var secondIndex = myData.MaxIndex();

            ArrayUtils.RemovePeakNeighbours(myData, secondIndex, filterRange);
            var thirdIndex = myData.MaxIndex();

            var firstFreq = (firstIndex + 1) * SAMPLE_RATE / FFT_SIZE;
            var secondFreq = (secondIndex + 1) * SAMPLE_RATE / FFT_SIZE;
            var thirdFreq = (thirdIndex + 1) * SAMPLE_RATE / FFT_SIZE;

            if (data[firstIndex] < 0.5)
            {
                firstFreq = secondFreq = thirdFreq = -1;
            }

            if (data[secondIndex] < 0.25)
            {
                secondFreq = thirdFreq = -1;
            }

            if (data[thirdIndex] < 0.125)
            {
                thirdFreq = -1;
            }

            Invoke(new MethodInvoker(delegate ()
            {
                _newFirstNote = NotesUtils.ComputeNote(firstFreq);
                _newSecondNote = NotesUtils.ComputeNote(secondFreq);
                _newThirdNote = NotesUtils.ComputeNote(thirdFreq);

                var previousNote = NotesUtils.GetPrevoiusNote(_newFirstNote);
                var nextNote = NotesUtils.GetNextNote(_newFirstNote);

                actualNoteLabel.Text = _newFirstNote.FullNoteName;
                previusNoteLabel.Text = previousNote.FullNoteName;
                nextNoteLabel.Text = nextNote.FullNoteName;
                firstTrackBar.Value = 50 + _newFirstNote.DifferenceFromReference;

                if (Math.Abs(_newFirstNote.DifferenceFromReference) > 10)
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

            //label1.Text = string.Format("Buffers captured: {0}", _buffersCaptured);
            //label2.Text = string.Format("Buffer size: {0}", values.Length);
            //label3.Text = string.Format("Unanalyzed values: {0}", unanalyzed_values.Count);
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
            PanelUtils.InitializeSigns(signs);

            PanelUtils.DrawNote(_firstNote, panel1, showSigns);
            PanelUtils.DrawNote(_secondNote, panel1, showSigns);
            PanelUtils.DrawNote(_thirdNote, panel1, showSigns);
        }

        private void timer2_Tick(object sender, EventArgs e)
        {
            if (!_newThirdNote.FullNoteName.Equals(_thirdNote.FullNoteName)
                || !_newSecondNote.FullNoteName.Equals(_secondNote.FullNoteName)
                || !_newFirstNote.FullNoteName.Equals(_firstNote.FullNoteName))
            {
                _firstNote = _newFirstNote;
                _secondNote = _newSecondNote;
                _thirdNote = _newThirdNote;

                this.Refresh();

                if (showSigns)
                {
                    RefreshSigns();
                }
                
            }
            
        }

        private void RefreshSigns()
        {
            foreach (var lst in signs)
            {
                lst.Value.Visible = false;
            }
        }


        public Dictionary<string, PictureBox> GetSharpsFlatsPicBoxes()
        {
            var ls = new Dictionary<string, PictureBox>();

            ls.Add("F#/Gb2", Fs2);
            ls.Add("F#/Gb3", Fs3);
            ls.Add("F#/Gb4", Fs4);
            ls.Add("F#/Gb5", Fs5);


            ls.Add("C#/Db3", Cs3);
            ls.Add("C#/Db4", Cs4);
            ls.Add("C#/Db5", Cs5);

            ls.Add("G#/Ab2", Ab2);
            ls.Add("G#/Ab3", Ab3);
            ls.Add("G#/Ab4", Ab4);

            ls.Add("A#/Bb2", Bb2);
            ls.Add("A#/Bb3", Bb3);
            ls.Add("A#/Bb4", Bb4);

            ls.Add("D#/Eb3", Eb3);
            ls.Add("D#/Eb4", Eb4);
            ls.Add("D#/Eb5", Eb5);

            foreach (var item in ls)
            {
                item.Value.BackColor = Color.Transparent;
            }

            return ls;
        }
    }
}
