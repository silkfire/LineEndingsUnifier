namespace LineEndingsUnifier
{
    public class LastChanges
    {
        public LastChanges(long ticks, LineEndingsChanger.LineEnding lineEnding)
        {
            Ticks = ticks;
            LineEnding = lineEnding;
        }

        public long Ticks { get; }

        public LineEndingsChanger.LineEnding LineEnding { get; }
    }
}
