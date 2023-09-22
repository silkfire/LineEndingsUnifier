namespace LineEndingsUnifier
{
    using EnvDTE;
    using EnvDTE80;
    using Microsoft.VisualStudio.Shell;

    using System;
    using System.ComponentModel;
    using System.IO;
    using System.Runtime.InteropServices;

    [ClassInterface(ClassInterfaceType.AutoDual)]
    internal class OptionsPage : DialogPage
    {
        private const string Category = "Line Endings Unifier";

        public const string ChangeLogFileExtension = "leu";

        private bool _trackChanges;

        [Category(Category)]
        [DisplayName("Default Line Ending")]
        [Description("The default line ending")]
        public LineEndingsChanger.LineEndingList DefaultLineEnding { get; set; } = LineEndingsChanger.LineEndingList.Windows;

        [Category(Category)]
        [DisplayName("Force Default Line Ending on Document Save")]
        [Description("Determines if line endings have to be unified automatically on a document save")]
        public bool ForceDefaultLineEndingOnSave { get; set; }


        private string _supportedFileFormats = ".cpp; .c; .h; .hpp; .cs; .js; .vb; .txt";

        [Category(Category)]
        [DisplayName("Supported File Formats")]
        [Description("Files with these formats will have line endings unified")]
        public string SupportedFileFormats
        {
            get => _supportedFileFormats;
            set
            {
                SupportedFileFormatsArray =  value.Replace(" ", "").Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                _supportedFileFormats = value;
            }
        }

        internal string[] SupportedFileFormatsArray { get; private set; }


        private string _supportedFilenames = "Dockerfile";

        [Category(Category)]
        [DisplayName("Supported Filenames")]
        [Description("Files with these names will have line endings unified")]
        public string SupportedFilenames
        {
            get => _supportedFilenames;
            set
            {
                SupportedFilenamesArray =  value.Replace(" ", "").Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                _supportedFilenames = value;
            }
        }

        internal string[] SupportedFilenamesArray { get; private set; }


        [Category(Category)]
        [DisplayName("Save Files After Unifying")]
        [Description("When you click \"Unify Line Endings In This...\" button, changed files won't be saved. Set this to TRUE if you want them to be automatically saved.")]
        public bool SaveFilesAfterUnifying { get; set; }

        [Category(Category)]
        [DisplayName("Write Report to the Output Window")]
        [Description("Set this to TRUE if you want the extension to write a report in the Output window")]
        public bool WriteReport { get; set; }

        [Category(Category)]
        [DisplayName("Unify Only Open Files on Save All")]
        [Description("Set this to TRUE if you want the extension to unify only files that are open in the editor after hitting \"Save All\"")]
        public bool UnifyOnlyOpenFiles { get; set; }

        [Category(Category)]
        [DisplayName("Add Newline on the Last Line")]
        [Description("Set this to TRUE if you want the extension to add a newline character on the last line when unifying line endings")]
        public bool AddNewlineOnLastLine { get; set; }

        [Category(Category)]
        [DisplayName("Track Changes")]
        [Description("Set this to TRUE if you want the extension to remember when files were unified to improve performance")]
        public bool TrackChanges
        {
            get => _trackChanges;
            set
            {
                ThreadHelper.ThrowIfNotOnUIThread();

                if (!value)
                {
                    if (ServiceProvider.GlobalProvider.GetService(typeof(DTE)) is DTE2 ide)
                    {
                        if (ide.Solution.FullName != "")
                        {
                            var path = $"{Path.GetDirectoryName(ide.Solution.FullName)}.{ChangeLogFileExtension}";

                            if (File.Exists(path)) File.Delete(path);
                        }
                    }
                }

                _trackChanges = value;
            }
        }

        [Category(Category)]
        [DisplayName("Remove Trailing Whitespace")]
        [Description("Set this to TRUE if you want the extension to remove trailing whitespace characters while unifying newline characters")]
        public bool RemoveTrailingWhitespace { get; set; }
    }
}
