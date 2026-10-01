namespace BeatTemplate
{
    public static class BeatMath
    {
        public static double BeatsToSeconds(double beats, double bpm) => beats * 60.0 / bpm;
        public static double SecondsToBeats(double sec, double bpm) => sec * bpm / 60.0;
        public static double SubSeconds(double bpm, int subPerBeat) => (60.0 / bpm) / subPerBeat;
    }
}