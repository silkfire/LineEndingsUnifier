namespace LineEndingsUnifier
{
    using EnvDTE;
    using EnvDTE80;
    using Microsoft.VisualStudio;
    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Shell.Interop;

    using System;
    using System.Collections.Generic;
    using System.ComponentModel.Design;
    using System.Diagnostics;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;

    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration("#110", "#112", "2.0", IconResourceID = 400)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [Guid(GuidList.guidLine_Endings_UnifierPkgString)]
    [ProvideOptionPage(typeof(OptionsPage), "Line Endings Unifier", "General Settings", 0, 0, true)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.SolutionExistsAndFullyLoaded_string, PackageAutoLoadFlags.BackgroundLoad)]
    public sealed class LineEndingsUnifierAsyncPackage: AsyncPackage
    {
        private static class LogStrings
        {
            public const string UnifyingStarted = "Unifying started...";
            public const string OperationResultTemplate = "{0}: changed {1} out of {2} line endings";
            public const string NoModificationRequiredTemplate = "{0}: no need to modify this file";
            public const string Done = "Done.";
            public const string DoneTemplate = "Done in {0} seconds.";
        }


        private RunningDocumentTable _runningDocumentTable;
        private DocumentSaveListener _documentSaveListener;
        private ChangesManager _changesManager;

        private bool _isUnifyingLocked;
        private Dictionary<string, LastChanges> _changeLog;

        private Guid _outputWindowGuid = new Guid("0F44E2D1-F5FA-4d2d-AB30-22BE8ECD9789");
        private IVsOutputWindow _outputWindow;

        private OptionsPage _optionsPage;
        private DTE2 _ide;


        private OptionsPage OptionsPage => _optionsPage ?? (_optionsPage = GetDialogPage(typeof(OptionsPage)) as OptionsPage);

        public DTE2 Ide
        {
            get
            {
                ThreadHelper.ThrowIfNotOnUIThread();

                return _ide ?? (_ide = GetService(typeof(DTE)) as DTE2);
            }
        }

        private LineEndingsChanger.LineEnding DefaultLineEnding => (LineEndingsChanger.LineEnding)OptionsPage.DefaultLineEnding;

        private string[] SupportedFileFormats
        {
            get { return OptionsPage.SupportedFileFormats.Replace(" ", "").Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries); }
        }

        private string[] SupportedFileNames
        {
            get { return OptionsPage.SupportedFileNames.Replace(" ", "").Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries); }
        }




        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            // runs in the background thread and doesn't affect the responsiveness of the UI thread.
            await Task.Delay(5000, cancellationToken);

            await base.InitializeAsync(cancellationToken, progress);

            // Switches to the UI thread in order to consume some services used in command initialization
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            if (await GetServiceAsync(typeof(IMenuCommandService)) is OleMenuCommandService mcs)
            {
                var commands = new List<(CommandID CommandId, EventHandler EventHandler)>
                {
                    (new CommandID(GuidList.guidLine_Endings_UnifierCmdSet_Solution, (int)PkgCmdIDList.cmdidUnifyLineEndings_Solution), UnifyLineEndingsInSolutionEventHandler),
                    (new CommandID(GuidList.guidLine_Endings_UnifierCmdSet_Folder,   (int)PkgCmdIDList.cmdidUnifyLineEndings_Folder),   UnifyLineEndingsInFolderEventHandler),
                    (new CommandID(GuidList.guidLine_Endings_UnifierCmdSet_Project,  (int)PkgCmdIDList.cmdidUnifyLineEndings_Project),  UnifyLineEndingsInProjectEventHandler),
                    (new CommandID(GuidList.guidLine_Endings_UnifierCmdSet_File,     (int)PkgCmdIDList.cmdidUnifyLineEndings_File),     UnifyLineEndingsInFileEventHandler),
                };

                foreach (var (commandId, eventHandler) in commands)
                {
                    mcs.AddCommand(new MenuCommand(eventHandler, commandId)
                    {
                        Visible = true,
                        Enabled = true
                    });
                }

                SetupOutputWindow();

                // ReSharper disable once SuspiciousTypeConversion.Global
                IServiceProvider serviceProvider = new ServiceProvider((Microsoft.VisualStudio.OLE.Interop.IServiceProvider)Ide);

                _runningDocumentTable = new RunningDocumentTable(serviceProvider);
                _documentSaveListener = new DocumentSaveListener(_runningDocumentTable);
                _documentSaveListener.BeforeSave += DocumentSaveListener_BeforeSave;
                _changesManager = new ChangesManager();
            }
            else
            {
                throw new COMException($"Unable to resolve service {nameof(IMenuCommandService)}.");
            }
        }

        private int DocumentSaveListener_BeforeSave(uint docCookie)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var document = GetDocumentFromDocCookie(docCookie);

            if (!_isUnifyingLocked)
            {
                if (OptionsPage.ForceDefaultLineEndingOnSave)
                {
                    var currentDocument = document;
                    var textDocument = currentDocument.Object("TextDocument") as TextDocument;

                    var supportedFileFormats = SupportedFileFormats;
                    var supportedFileNames = SupportedFileNames;

                    if (currentDocument.Name.EndsWithAny(supportedFileFormats) || currentDocument.Name.EqualsAny(supportedFileNames))
                    {
                        Output($"{LogStrings.UnifyingStarted}\n");
                        var numberOfChanges = 0;
                        UnifyLineEndingsInDocument(textDocument, DefaultLineEnding, ref numberOfChanges, out var numberOfIndividualChanges, out var numberOfAllLineEndings);
                        Output(string.Format($"{LogStrings.OperationResultTemplate}\n", currentDocument.FullName, numberOfIndividualChanges, numberOfAllLineEndings));
                        Output($"{LogStrings.Done}\n");
                    }
                }
            }

            return VSConstants.S_OK;
        }


        private void UnifyLineEndingsInSolutionEventHandler(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var currentSolution = Ide.Solution;

            string solutionName = null;
            foreach (Property property in currentSolution.Properties)
            {
                if (property.Name == "Name")
                {
                    solutionName = property.Value.ToString();

                    break;
                }
            }

            if (solutionName == null) throw new InvalidOperationException("Unable to get the name of the current solution.");

            UnifyLineEndingsFromSolutionExplorerMenuCommand(solutionName, UnifyOperation);


            int UnifyOperation(LineEndingsChanger.LineEnding lineEndings)
            {
                var numberOfChanges = 0;

                foreach (var project in currentSolution.GetAllProjects())
                {
                    UnifyLineEndingsInProjectItems(project.ProjectItems, lineEndings, ref numberOfChanges);
                }

                return numberOfChanges;
            }
        }

        private void UnifyLineEndingsInFolderEventHandler(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var selectedFolder = Ide.SelectedItems.Item(1).ProjectItem;

            UnifyLineEndingsFromSolutionExplorerMenuCommand(selectedFolder.Name, UnifyOperation);

            int UnifyOperation(LineEndingsChanger.LineEnding lineEndings)
            {
                var numberOfChanges = 0;

                UnifyLineEndingsInProjectItems(selectedFolder.ProjectItems, lineEndings, ref numberOfChanges);

                return numberOfChanges;
            }
        }

        private void UnifyLineEndingsInProjectEventHandler(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var selectedProject = Ide.SelectedItems.Item(1).Project;

            UnifyLineEndingsFromSolutionExplorerMenuCommand(selectedProject.Name, UnifyOperation);

            int UnifyOperation(LineEndingsChanger.LineEnding lineEndings)
            {
                var numberOfChanges = 0;

                UnifyLineEndingsInProjectItems(selectedProject.ProjectItems, lineEndings, ref numberOfChanges);

                return numberOfChanges;
            }
        }

        private void UnifyLineEndingsInFileEventHandler(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var selectedFile = Ide.SelectedItems.Item(1).ProjectItem;

            UnifyLineEndingsFromSolutionExplorerMenuCommand(selectedFile.Name, UnifyOperation, selectedFile.Name.EndsWithAny(SupportedFileFormats) || selectedFile.Name.EqualsAny(SupportedFileNames));

            int UnifyOperation(LineEndingsChanger.LineEnding lineEndings)
            {
                var numberOfChanges = 0;

                UnifyLineEndingsInProjectItem(selectedFile, lineEndings, ref numberOfChanges);

                return numberOfChanges;
            }
        }

        private void UnifyLineEndingsFromSolutionExplorerMenuCommand(string windowTitle, Func<LineEndingsChanger.LineEnding, int> unifyOperation, bool unifyCondition = true)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var choiceWindow = new LineEndingChoice(windowTitle, DefaultLineEnding);
            if (choiceWindow.ShowDialog() == true && choiceWindow.LineEnding != LineEndingsChanger.LineEnding.None)
            {
                if (unifyCondition)
                {
                    _ = JoinableTaskFactory.RunAsync(async () =>
                    {
                        await JoinableTaskFactory.SwitchToMainThreadAsync();

                        Output($"{LogStrings.UnifyingStarted}\n");

                        if (OptionsPage.TrackChanges) _changeLog = _changesManager.GetLastChanges(Ide.Solution);

                        var stopWatch = new Stopwatch();
                        stopWatch.Start();
                        var numberOfChanges = unifyOperation(choiceWindow.LineEnding);
                        stopWatch.Stop();
                        var secondsElapsed = stopWatch.ElapsedMilliseconds / 1000.0;

                        if (OptionsPage.TrackChanges) _changesManager.SaveLastChanges(Ide.Solution, _changeLog);

                        _changeLog = null;
                        Output($"{string.Format(LogStrings.DoneTemplate, secondsElapsed)}\n");
                    });
                }
            }
        }


        private void UnifyLineEndingsInProjectItems(ProjectItems projectItems, LineEndingsChanger.LineEnding lineEnding, ref int numberOfChanges)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var supportedFileFormats = SupportedFileFormats;
            var supportedFileNames = SupportedFileNames;

            foreach (ProjectItem item in projectItems)
            {
                if (item.ProjectItems != null && item.ProjectItems.Count > 0)
                {
                    UnifyLineEndingsInProjectItems(item.ProjectItems, lineEnding, ref numberOfChanges);
                }

                if (item.Name.EndsWithAny(supportedFileFormats) || item.Name.EqualsAny(supportedFileNames))
                {
                    UnifyLineEndingsInProjectItem(item, lineEnding, ref numberOfChanges);
                }
            }
        }

        private void UnifyLineEndingsInProjectItem(ProjectItem item, LineEndingsChanger.LineEnding lineEnding, ref int numberOfChanges)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            Window documentWindow = null;

            if (!item.IsOpen)
            {
                if (!OptionsPage.UnifyOnlyOpenFiles)
                {
                    documentWindow = item.Open();
                }
            }

            var document = item.Document;
            if (document != null)
            {
                var trackChanges = OptionsPage.TrackChanges && _changeLog != null && (   !_changeLog.ContainsKey(document.FullName)
                                                                                      ||  _changeLog[document.FullName].LineEnding != lineEnding
                                                                                      ||  _changeLog[document.FullName].Ticks < File.GetLastWriteTime(document.FullName).Ticks);

                if (!OptionsPage.TrackChanges || trackChanges)
                {
                    var textDocument = document.Object("TextDocument") as TextDocument;
                    UnifyLineEndingsInDocument(textDocument, lineEnding, ref numberOfChanges, out var numberOfIndividualChanges, out var numberOfAllLineEndings);
                    if (documentWindow != null || OptionsPage.SaveFilesAfterUnifying)
                    {
                        _isUnifyingLocked = true;
                        document.Save();
                        _isUnifyingLocked = false;
                    }

                    if (trackChanges) _changeLog[document.FullName] = new LastChanges(DateTime.Now.Ticks, lineEnding);

                    Output(string.Format($"{LogStrings.OperationResultTemplate}\n", document.FullName, numberOfIndividualChanges, numberOfAllLineEndings));
                }
                else
                {
                    Output(string.Format($"{LogStrings.NoModificationRequiredTemplate}\n", document.FullName));
                }
            }

            documentWindow?.Close();
        }

        private void UnifyLineEndingsInDocument(TextDocument textDocument, LineEndingsChanger.LineEnding lineEnding, ref int numberOfChanges, out int numberOfIndividualChanges, out int numberOfAllLineEndings)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var startPoint = textDocument.StartPoint.CreateEditPoint();
            var endPoint = textDocument.EndPoint.CreateEditPoint();

            var text = startPoint.GetText(endPoint.AbsoluteCharOffset);
            var originalLength = text.Length;

            if (OptionsPage.RemoveTrailingWhitespace)
            {
                text = TrailingWhitespaceRemover.RemoveTrailingWhitespace(text);
            }

            var changedText = LineEndingsChanger.ChangeLineEndings(text, lineEnding, ref numberOfChanges, out numberOfIndividualChanges, out numberOfAllLineEndings);

            if (OptionsPage.AddNewlineOnLastLine)
            {
                if (!changedText.EndsWith(Utilities.GetNewlineString(lineEnding)))
                {
                    changedText += Utilities.GetNewlineString(lineEnding);
                }
            }

            startPoint.ReplaceText(originalLength, changedText, (int)vsEPReplaceTextOptions.vsEPReplaceTextKeepMarkers);
        }

        private void SetupOutputWindow()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var outputWindowService = ServiceProvider.GlobalProvider.GetService(typeof(SVsOutputWindow));

            if (outputWindowService == null) throw new COMException($"Unable to resolve service {nameof(SVsOutputWindow)}.");

            if (!(outputWindowService is IVsOutputWindow outputWindow)) throw new InvalidCastException($"Unable to cast object of type '{nameof(Microsoft)}{nameof(Microsoft.VisualStudio)}{nameof(Microsoft.VisualStudio.Shell)}{nameof(Microsoft.VisualStudio.Shell.Interop)}{nameof(SVsOutputWindow)}' to type '{nameof(Microsoft)}{nameof(Microsoft.VisualStudio)}{nameof(Microsoft.VisualStudio.Shell)}{nameof(Microsoft.VisualStudio.Shell.Interop)}{nameof(IVsOutputWindow)}'.");

            _outputWindow = outputWindow;
            _outputWindow.CreatePane(ref _outputWindowGuid, "Line Endings Unifier", 1, 1);
        }

        private void Output(string message)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (!OptionsPage.WriteReport) return;

            _outputWindow.GetPane(ref _outputWindowGuid, out var outputWindowPane);

            outputWindowPane.OutputStringThreadSafe(message);
        }

        private Document GetDocumentFromDocCookie(uint docCookie)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var documentInfoMoniker = _runningDocumentTable.GetDocumentInfo(docCookie).Moniker;

            foreach (Document document in Ide.Documents)
            {
                if (document.FullName == documentInfoMoniker) return document;
            }

            return null;
        }
    }
}
