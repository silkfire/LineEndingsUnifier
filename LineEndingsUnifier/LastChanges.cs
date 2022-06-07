namespace LineEndingsUnifier
{
    public class LastChanges
    {
        public LastChanges(long ticks, LineEndingsChanger.LineEndings lineEndings)
        {
            Ticks = ticks;
            LineEndings = lineEndings;
        }

        public long Ticks { get; }

        public LineEndingsChanger.LineEndings LineEndings { get; }
    }
}
