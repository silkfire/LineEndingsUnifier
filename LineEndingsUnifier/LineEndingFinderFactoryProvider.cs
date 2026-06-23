namespace LineEndingsUnifier
{
    using Microsoft.VisualStudio.Text.Operations;

    internal sealed class LineEndingFinderFactoryProvider
    {
        private readonly IFinderFactory _lineEndingFinderFactory;
        private readonly IFinderFactory _consecutiveWhiteSpaceFollowedByLineEndingFinderFactory;

        private readonly IFinderFactory _nonWindowsLineEndingFinderFactory;
        private readonly IFinderFactory _nonLinuxLineEndingFinderFactory;
        private readonly IFinderFactory _nonMacintoshLineEndingFinderFactory;

        public LineEndingFinderFactoryProvider(IFindService findService)
        {
            _lineEndingFinderFactory                                = findService.CreateFinderFactory(LineEndingSearchPattern.Any,                                FindOptions.UseRegularExpressions);
            _consecutiveWhiteSpaceFollowedByLineEndingFinderFactory = findService.CreateFinderFactory(LineEndingSearchPattern.ConsecutiveWhiteSpaceFollowedByAny, FindOptions.UseRegularExpressions);

            _nonWindowsLineEndingFinderFactory                      = findService.CreateFinderFactory(LineEndingSearchPattern.NonWindows,                         FindOptions.UseRegularExpressions);
            _nonLinuxLineEndingFinderFactory                        = findService.CreateFinderFactory(LineEndingSearchPattern.NonLinux,                           FindOptions.UseRegularExpressions);
            _nonMacintoshLineEndingFinderFactory                    = findService.CreateFinderFactory(LineEndingSearchPattern.NonMacintosh,                       FindOptions.UseRegularExpressions);
        }

        public IFinderFactory GetLineEndingFinderFactory() => _lineEndingFinderFactory;
        public IFinderFactory GetConsecutiveWhiteSpaceFollowedByLineEndingFinderFactory() => _consecutiveWhiteSpaceFollowedByLineEndingFinderFactory;

        public IFinderFactory GetNonWindowsLineEndingFinderFactory() => _nonWindowsLineEndingFinderFactory;
        public IFinderFactory GetNonLinuxLineEndingFinderFactory() => _nonLinuxLineEndingFinderFactory;
        public IFinderFactory GetNonMacintoshLineEndingFinderFactory() => _nonMacintoshLineEndingFinderFactory;
    }
}
