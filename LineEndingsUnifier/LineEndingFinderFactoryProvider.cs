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

        private readonly IFinderFactory _nonWindowsLineEndingFinderFactory;
        private readonly IFinderFactory _nonLinuxLineEndingFinderFactory;
        private readonly IFinderFactory _nonMacintoshLineEndingFinderFactory;

        public LineEndingFinderFactoryProvider(IFindService findService)
        {
            _lineEndingFinderFactory                                = findService.CreateFinderFactory(LineEndingSearchPattern.Any,                                FindOptions.UseRegularExpressions);
            _consecutiveWhiteSpaceFollowedByLineEndingFinderFactory = findService.CreateFinderFactory(LineEndingSearchPattern.ConsecutiveWhiteSpaceFollowedByAny, FindOptions.UseRegularExpressions);

            _windowsLineEndingFinderFactory                         = findService.CreateFinderFactory(LineEndingSearchPattern.Windows,                            FindOptions.UseRegularExpressions);
            _linuxLineEndingFinderFactory                           = findService.CreateFinderFactory(LineEndingSearchPattern.Linux,                              FindOptions.UseRegularExpressions);
            _macintoshLineEndingFinderFactory                       = findService.CreateFinderFactory(LineEndingSearchPattern.Macintosh,                          FindOptions.UseRegularExpressions);

            _nonWindowsLineEndingFinderFactory                      = findService.CreateFinderFactory(LineEndingSearchPattern.NonWindows,                         FindOptions.UseRegularExpressions);
            _nonLinuxLineEndingFinderFactory                        = findService.CreateFinderFactory(LineEndingSearchPattern.NonLinux,                           FindOptions.UseRegularExpressions);
            _nonMacintoshLineEndingFinderFactory                    = findService.CreateFinderFactory(LineEndingSearchPattern.NonMacintosh,                       FindOptions.UseRegularExpressions);
        }

        public IFinderFactory GetLineEndingFinderFactory() => _lineEndingFinderFactory;
        public IFinderFactory GetConsecutiveWhiteSpaceFollowedByLineEndingFinderFactory() => _consecutiveWhiteSpaceFollowedByLineEndingFinderFactory;

        public IFinderFactory GetWindowsLineEndingFinderFactory() => _windowsLineEndingFinderFactory;
        public IFinderFactory GetLinuxLineEndingFinderFactory() => _linuxLineEndingFinderFactory;
        public IFinderFactory GetMacintoshLineEndingFinderFactory() => _macintoshLineEndingFinderFactory;

        public IFinderFactory GetNonWindowsLineEndingFinderFactory() => _nonWindowsLineEndingFinderFactory;
        public IFinderFactory GetNonLinuxLineEndingFinderFactory() => _nonLinuxLineEndingFinderFactory;
        public IFinderFactory GetNonMacintoshLineEndingFinderFactory() => _macintoshLineEndingFinderFactory;
    }
}
