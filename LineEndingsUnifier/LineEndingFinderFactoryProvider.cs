namespace LineEndingsUnifier
{
    using Microsoft.VisualStudio.Text.Operations;

    internal class LineEndingFinderFactoryProvider
    {
        private readonly IFinderFactory _lineEndingFinderFactory;
        private readonly IFinderFactory _consecutiveWhiteSpaceFollowedByLineEndingFinderFactory;

        private readonly IFinderFactory _windowsLineEndingFinderFactory;
        private readonly IFinderFactory _linuxLineEndingFinderFactory;
        private readonly IFinderFactory _macintoshLineEndingFinderFactory;

        public LineEndingFinderFactoryProvider(IFindService findService)
        {
            _lineEndingFinderFactory                                = findService.CreateFinderFactory(LineEndingSearchPattern.Any,                                FindOptions.UseRegularExpressions);
            _consecutiveWhiteSpaceFollowedByLineEndingFinderFactory = findService.CreateFinderFactory(LineEndingSearchPattern.ConsecutiveWhiteSpaceFollowedByAny, FindOptions.UseRegularExpressions);

            _windowsLineEndingFinderFactory                         = findService.CreateFinderFactory(LineEndingSearchPattern.Windows,                            FindOptions.UseRegularExpressions);
            _linuxLineEndingFinderFactory                           = findService.CreateFinderFactory(LineEndingSearchPattern.Linux,                              FindOptions.UseRegularExpressions);
            _macintoshLineEndingFinderFactory                       = findService.CreateFinderFactory(LineEndingSearchPattern.Macintosh,                          FindOptions.UseRegularExpressions);
        }

        public IFinderFactory GetLineEndingFinderFactory() => _lineEndingFinderFactory;
        public IFinderFactory GetConsecutiveWhiteSpaceFollowedByLineEndingFinderFactory() => _consecutiveWhiteSpaceFollowedByLineEndingFinderFactory;

        public IFinderFactory GetWindowsLineEndingFinderFactory() => _windowsLineEndingFinderFactory;
        public IFinderFactory GetLinuxLineEndingFinderFactory() => _linuxLineEndingFinderFactory;
        public IFinderFactory GetMacinotshLineEndingFinderFactory() => _macintoshLineEndingFinderFactory;
    }
}
