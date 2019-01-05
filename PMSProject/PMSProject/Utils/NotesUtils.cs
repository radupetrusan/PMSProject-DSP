using PMSProject.Models;
using System;

namespace PMSProject.Utils
{
    public static class NotesUtils
    {
        public static string[] _noteNames = new string[] { "C", "C#/Db", "D", "D#/Eb", "E", "F", "F#/Gb", "G", "G#/Ab", "A", "A#/Bb", "B" };

        private static double BASE_FREQ = 16.35;
        private static double TWELTH_ROOT_OF_2 = 1.0594630943592952645618252949463; // 2 ^ (1/12)

        public static Note ComputeNote(int frequency)
        {
            if (frequency < 16 || frequency > 8000)
            {
                return new Note()
                {
                    NoteName = "C",
                    Scale = 0
                };
            }

            double stepsFromC0 = 0;
            double semitonesFromRef = 0;
            int octaveFromC0 = 0;
            string actualNote = string.Empty;
            int noteIndex = 0;

            int scale = 0; /* Refference scale (C0, D1, E1 ... ) */

            stepsFromC0 = Math.Log((frequency / BASE_FREQ), TWELTH_ROOT_OF_2);

            // Round to 3 decimals.
            stepsFromC0 = Math.Round(stepsFromC0, 2);
            octaveFromC0 = (int)Math.Floor(stepsFromC0 / 12);

            if (Math.Floor(Math.Round(stepsFromC0) / 12) != octaveFromC0)
            {
                scale = octaveFromC0 + 1;
            }
            else
            {
                scale = octaveFromC0;
            }

            // Here we calculate the number of semitones from our refference            
            semitonesFromRef = stepsFromC0 - (12 * octaveFromC0);
            noteIndex = (int)Math.Round(semitonesFromRef);

            var differenceFromReference = (int)((semitonesFromRef - noteIndex) * 100);

            if (noteIndex == 12)
            {
                noteIndex = 0;
            }

            // Final note extracted from notes_string array.
            actualNote = _noteNames[noteIndex];

            return new Note()
            {
                NoteName = actualNote,
                Scale = scale,
                DifferenceFromReference = differenceFromReference,
                Index = noteIndex
            };
        }

        public static Note GetPrevoiusNote(Note note)
        {
            var name = _noteNames[(note.Index + 12 - 1) % 12];
            
            var previousNote = new Note()
            {
                NoteName = name
            };

            previousNote.Scale = note.Index == 0 ? note.Scale - 1 : note.Scale;

            return previousNote;
        }

        public static Note GetNextNote(Note note)
        {
            var name = _noteNames[(note.Index + 1) % 12];

            var nextNote = new Note()
            {
                NoteName = name
            };

            nextNote.Scale = note.Index == 11 ? note.Scale + 1 : note.Scale;

            return nextNote;
        }
    }
}
