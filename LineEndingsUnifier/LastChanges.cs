namespace LineEndingsUnifier
{
    internal readonly struct LastChanges
    {
        public long Ticks { get; }

        public LineEndingsChanger.LineEnding LineEnding { get; }


        public LastChanges(long ticks, LineEndingsChanger.LineEnding lineEnding)
        {
            Ticks = ticks;
            LineEnding = lineEnding;
        }
    }
}
