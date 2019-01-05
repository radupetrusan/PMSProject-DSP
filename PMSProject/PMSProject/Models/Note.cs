namespace PMSProject.Models
{
    public class Note
    {
        public string NoteName { get; set; }

        public int Scale { get; set; }

        public int DifferenceFromReference { get; set; }

        public int Index { get; set; }

        public string FullNoteName {
            get
            {
                return NoteName + Scale;
            }
        }

    }
}
