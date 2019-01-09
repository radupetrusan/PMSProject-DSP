using PMSProject.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PMSProject.Utils
{
    public static class PanelUtils
    {
        // Constans
        private static int STAFF_HEIGHT = 15;
        private static int NOTE_HEIGHT = 14;
        private static int NOTE_WIDTH = 20;
        private static Pen NOTE_PEN = new Pen(Color.Black, 2);
        //private static Brush NOTE_BRUSH = Brushes.Black;
        public static Boolean ADDITIONAL_BAR = true;
        public static int NOTE_DISTANCTE = 120;

        private static Graphics _graphics;

        private static Dictionary<string, PictureBox> _signList;

        public static void InitializeGraphics(Graphics graphics)
        {
            _graphics = graphics;
            _graphics.SmoothingMode = SmoothingMode.HighQuality;
        }

        public static void InitializeSigns(Dictionary<string, PictureBox> list)
        {
            _signList = list;
        }


        public static void InitializePanel(Panel musicPanel, Note note)
        {
            if (_graphics == null)
            {
                return;
            }

            // draw some staff lines
            for (int i = 1; i < 13; i++)
            {
                if (i != 6 && i != 12)
                {
                    _graphics.DrawLine(Pens.Black, 15, i * STAFF_HEIGHT, musicPanel.Width, i * STAFF_HEIGHT);
                }
            }

            //set all sharps/flats as invisible

        }

        public static void DrawNote(Note note, Panel musicPanel)
        {
            if (note == null || musicPanel == null)
            {
                return;
            }

            if (ADDITIONAL_BAR == true)
            {
                if (note.NoteName == "C" && note.Scale == 4)
                {
                    _graphics.DrawLine(NOTE_PEN, NOTE_DISTANCTE - 5, 6 * STAFF_HEIGHT, NOTE_DISTANCTE + NOTE_WIDTH + 5, 6 * STAFF_HEIGHT);

                }
                else if (note.NoteName == "E" && note.Scale == 2)
                {
                    _graphics.DrawLine(Pens.Black, NOTE_DISTANCTE - 5, 12 * STAFF_HEIGHT, NOTE_DISTANCTE + NOTE_WIDTH + 5, 12 * STAFF_HEIGHT);
                }
            }

            InitializePanel(musicPanel, note);

            var scale = note.Scale;
            var noteName = note.NoteName;

            double spaceOfTheNote = 1;

            if (2 <= scale && scale <= 5)
            {
                try {
                    _signList[note.FullNoteName].Visible = true;
                }
                catch
                {

                }


                switch (scale)
                {
                    case 2:
                        if (noteName != "C" && noteName != "C#/Db" && noteName != "D" && noteName != "D#/Eb")
                        {
                            switch (noteName)
                            {
                                case "E": spaceOfTheNote = 11.5; break;
                                case "F": spaceOfTheNote = 11; break;
                                case "F#/Gb": spaceOfTheNote = 11; break;
                                case "G": spaceOfTheNote = 10.5; break;
                                case "G#/Ab": spaceOfTheNote = 10; break;
                                case "A": spaceOfTheNote = 10; break;
                                case "A#/Bb": spaceOfTheNote = 9.5; break;
                                case "B": spaceOfTheNote = 9.5; break;
                                default: break;
                            }
                        }

                        break;
                    case 3:
                        switch (noteName)
                        {
                            case "C": spaceOfTheNote = 9; break;
                            case "C#/Db": spaceOfTheNote = 9; break;
                            case "D": spaceOfTheNote = 8.5; break;
                            case "D#/Eb": spaceOfTheNote = 8; break;
                            case "E": spaceOfTheNote = 8; break;
                            case "F": spaceOfTheNote = 7.5; break;
                            case "F#/Gb": spaceOfTheNote = 7.5; break;
                            case "G": spaceOfTheNote = 7; break;
                            case "G#/Ab": spaceOfTheNote = 6.5; break;
                            case "A": spaceOfTheNote = 6.5; break;
                            case "A#/Bb": spaceOfTheNote = 6; break;
                            case "B": spaceOfTheNote = 6; break;

                        }
                        break;
                    case 4:
                        switch (noteName)
                        {
                            case "C": spaceOfTheNote = 5.5; break;
                            case "C#/Db": spaceOfTheNote = 5; break;
                            case "D": spaceOfTheNote = 5; break;
                            case "D#/Eb": spaceOfTheNote = 4.5; break;
                            case "E": spaceOfTheNote = 4.5; break;
                            case "F": spaceOfTheNote = 4; break;
                            case "F#/Gb": spaceOfTheNote = 4; break;
                            case "G": spaceOfTheNote = 3.5; break;
                            case "G#/Ab": spaceOfTheNote = 3; break;
                            case "A": spaceOfTheNote = 3; break;
                            case "A#/Bb": spaceOfTheNote = 2.5; break;
                            case "B": spaceOfTheNote = 2.5; break;
                            default: break;
                        }
                        break;
                    case 5:
                        if (noteName != "G#/Ab" && noteName != "A" && noteName != "A#/Bb" && noteName != "B")
                        {
                            switch (noteName)
                            {
                                case "C": spaceOfTheNote = 2; break;
                                case "C#/Db": spaceOfTheNote = 2; break;
                                case "D": spaceOfTheNote = 1.5; break;
                                case "D#/Eb": spaceOfTheNote = 1; break;
                                case "E": spaceOfTheNote = 1; break;
                                case "F": spaceOfTheNote = 0.5; break;
                                case "F#/Gb": spaceOfTheNote = 0.5; break;
                                case "G": spaceOfTheNote = 0; break;
                                default: break;
                            }
                        }
                        break;

                    default: break;
                }
            }

            if (2 <= scale && scale <= 5)
            {
                switch (scale)
                {
                    case 2:
                        if (noteName != "C" && noteName != "C#/Db" && noteName != "D" && noteName != "D#/Eb")
                        {
                            _graphics.DrawEllipse(NOTE_PEN, NOTE_DISTANCTE, (int)(spaceOfTheNote * STAFF_HEIGHT), NOTE_WIDTH, NOTE_HEIGHT);
                        }
                        break;
                    case 5:
                        if (noteName != "G#/Ab" && noteName != "A" && noteName != "A#/Bb" && noteName != "B")
                        {
                            _graphics.DrawEllipse(NOTE_PEN, NOTE_DISTANCTE, (int)(spaceOfTheNote * STAFF_HEIGHT), NOTE_WIDTH, NOTE_HEIGHT);
                        }
                        break;
                    default:
                        _graphics.DrawEllipse(NOTE_PEN, NOTE_DISTANCTE, (int)(spaceOfTheNote * STAFF_HEIGHT), NOTE_WIDTH, NOTE_HEIGHT);
                        break;
                }
            }
        }

    }
}
