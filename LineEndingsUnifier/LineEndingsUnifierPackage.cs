namespace LineEndingsUnifier
{
    using static LineEndingsChanger;

    using EnvDTE;
    using EnvDTE80;
    using Microsoft.VisualStudio;
    using Microsoft.VisualStudio.ComponentModelHost;
    using Microsoft.VisualStudio.Editor;
    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Shell.Interop;
    using Microsoft.VisualStudio.Text;
    using Microsoft.VisualStudio.Text.Operations;
    using Microsoft.VisualStudio.TextManager.Interop;

    using System;
    using System.Collections.Generic;
    using System.ComponentModel.Design;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
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

        private DTE2 _ide;
        private IComponentModel _componentModel;

        private LineEndingFinderFactoryProvider _lineEndingFinderFactoryProvider;

        private Guid _outputWindowGuid = new Guid("0F44E2D1-F5FA-4d2d-AB30-22BE8ECD9789");
        private IVsOutputWindow _outputWindow;

        private OptionsPage _optionsPage;


        private OptionsPage OptionsPage => _optionsPage ?? (_optionsPage = GetDialogPage(typeof(OptionsPage)) as OptionsPage);

        private LineEnding DefaultLineEnding => (LineEnding)OptionsPage.DefaultLineEnding;


        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            // runs in the background thread and doesn't affect the responsiveness of the UI thread.
            await Task.Delay(5_000, cancellationToken);

            await base.InitializeAsync(cancellationToken, progress);

            // Switches to the UI thread in order to consume some services used in command initialization
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            if (await GetServiceAsync(typeof(DTE)) is DTE2 dte)
            {
                _ide = dte;
            }
            else
            {
                throw new COMException($"Unable to resolve service {nameof(DTE2)}");
            }

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
                
            }
            else
            {
                throw new COMException($"Unable to resolve service {nameof(IMenuCommandService)}");
            }

            if (await GetServiceAsync(typeof(SVsOutputWindow)) is IVsOutputWindow outputWindow)
            {
                _outputWindow = outputWindow;
                _outputWindow.CreatePane(ref _outputWindowGuid, "Line Endings Unifier", 1, 1);
            }
            else
            {
                throw new COMException($"Unable to resolve service {nameof(IVsOutputWindow)}");
            }

            if (await GetServiceAsync(typeof(SComponentModel)) is IComponentModel componentModel)
            {
                _componentModel = componentModel;
            }
            else
            {
                throw new COMException($"Unable to resolve service {nameof(IComponentModel)}");
            }

            var findService = _componentModel.GetService<IFindService>();
            if (findService == null) throw new COMException($"Unable to resolve service {nameof(IFindService)}");
            _lineEndingFinderFactoryProvider = new LineEndingFinderFactoryProvider(findService);

            // ReSharper disable once SuspiciousTypeConversion.Global
            IServiceProvider serviceProvider = new ServiceProvider(_ide as Microsoft.VisualStudio.OLE.Interop.IServiceProvider);

            _runningDocumentTable = new RunningDocumentTable(serviceProvider);
            _documentSaveListener = new DocumentSaveListener(_runningDocumentTable);
            _documentSaveListener.BeforeSave += DocumentSaveListener_BeforeSave;
            _changesManager = new ChangesManager();
        }



        private int DocumentSaveListener_BeforeSave(uint docCookie)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (!_isUnifyingLocked)
            {
                if (OptionsPage.ForceDefaultLineEndingOnSave)
                {
                    var currentDocument = GetDocumentFromDocCookie(docCookie);
                    var textDocument = currentDocument.Object("TextDocument") as TextDocument;

                    if (DocumentMatchesConfiguredFileFormatsOrFilenames(currentDocument.Name))
                    {
                        var writeReport = OptionsPage.WriteReport;

                        if (writeReport) Output($"{LogStrings.UnifyingStarted}\n");

                        UnifyLineEndingsInDocument(textDocument, DefaultLineEnding, out var numberOfIndividualChanges, out var numberOfAllLineEndings, writeReport);

                        if (writeReport)
                        {
                            Output(string.Format($"{LogStrings.OperationResultTemplate}\n", currentDocument.FullName, numberOfIndividualChanges, numberOfAllLineEndings));
                            Output($"{LogStrings.Done}\n");
                        }
                    }
                }
            }

            return VSConstants.S_OK;
        }


        private void UnifyLineEndingsInSolutionEventHandler(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var currentSolution = _ide.Solution;

            string solutionName = null;
            foreach (Property property in currentSolution.Properties)
            {
                if (property.Name == "Name")
                {
                    solutionName = property.Value.ToString();

                    break;
                }
            }

            if (solutionName == null) throw new InvalidOperationException("Unable to get the name of the current solution");

            UnifyLineEndingsFromSolutionExplorerMenuCommand(solutionName, UnifyOperation);


            void UnifyOperation(LineEnding lineEndings)
            {
                foreach (var project in currentSolution.GetAllProjects())
                {
                    UnifyLineEndingsInProjectItems(project.ProjectItems, lineEndings);
                }
            }
        }

        private void UnifyLineEndingsInFolderEventHandler(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var selectedFolder = _ide.SelectedItems.Item(1).ProjectItem;

            UnifyLineEndingsFromSolutionExplorerMenuCommand(selectedFolder.Name, UnifyOperation);

            void UnifyOperation(LineEnding lineEndings)
            {
                UnifyLineEndingsInProjectItems(selectedFolder.ProjectItems, lineEndings);
            }
        }

        private void UnifyLineEndingsInProjectEventHandler(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var selectedProject = _ide.SelectedItems.Item(1).Project;

            UnifyLineEndingsFromSolutionExplorerMenuCommand(selectedProject.Name, UnifyOperation);

            void UnifyOperation(LineEnding lineEndings)
            {
                UnifyLineEndingsInProjectItems(selectedProject.ProjectItems, lineEndings);
            }
        }

        private void UnifyLineEndingsInFileEventHandler(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var selectedFile = _ide.SelectedItems.Item(1).ProjectItem;

            UnifyLineEndingsFromSolutionExplorerMenuCommand(selectedFile.Name, UnifyOperation, DocumentMatchesConfiguredFileFormatsOrFilenames(selectedFile.Name));

            void UnifyOperation(LineEnding lineEndings)
            {
                UnifyLineEndingsInProjectItem(selectedFile, lineEndings);
            }
        }

        private void UnifyLineEndingsFromSolutionExplorerMenuCommand(string windowTitle, Action<LineEnding> unifyOperation, bool unifyCondition = true)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var choiceWindow = new LineEndingChoice(windowTitle, DefaultLineEnding);
            if (choiceWindow.ShowDialog() == true && choiceWindow.LineEnding != LineEnding.None)
            {
                if (unifyCondition)
                {
                    _ = JoinableTaskFactory.RunAsync(async () =>
                    {
                        await JoinableTaskFactory.SwitchToMainThreadAsync();

                        var writeReport = OptionsPage.WriteReport;
                        var trackChanges = OptionsPage.TrackChanges;

                        if (writeReport) Output($"{LogStrings.UnifyingStarted}\n");
                        if (trackChanges) _changeLog = _changesManager.GetLastChanges(_ide.Solution);

                        Stopwatch sw = null;
                        if (writeReport)
                        {
                            sw = new Stopwatch();
                            sw.Start();
                        }

                        unifyOperation(choiceWindow.LineEnding);

                        double? secondsElapsed = null;
                        if (writeReport)
                        {
                            sw.Stop();
                            secondsElapsed = sw.ElapsedMilliseconds / 1000.0;
                        }

                        if (trackChanges) _changesManager.SaveLastChanges(_ide.Solution, _changeLog);
                        _changeLog = null;

                        if (writeReport) Output($"{string.Format(LogStrings.DoneTemplate, secondsElapsed.Value)}\n");
                    });
                }
            }
        }

        private void UnifyLineEndingsInProjectItems(ProjectItems projectItems, LineEnding lineEnding)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            foreach (ProjectItem item in projectItems)
            {
                if (item.ProjectItems != null && item.ProjectItems.Count > 0)
                {
                    UnifyLineEndingsInProjectItems(item.ProjectItems, lineEnding);
                }

                if (DocumentMatchesConfiguredFileFormatsOrFilenames(item.Name))
                {
                    UnifyLineEndingsInProjectItem(item, lineEnding);
                }
            }
        }

        private void UnifyLineEndingsInProjectItem(ProjectItem item, LineEnding lineEnding)
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

                var writeReport = OptionsPage.WriteReport;

                if (!OptionsPage.TrackChanges || trackChanges)
                {

                    var textDocument = document.Object("TextDocument") as TextDocument;
                    UnifyLineEndingsInDocument(textDocument, lineEnding, out var numberOfIndividualChanges, out var numberOfAllLineEndings, writeReport);
                    if (documentWindow != null || OptionsPage.SaveFilesAfterUnifying)
                    {
                        _isUnifyingLocked = true;
                        document.Save();
                        _isUnifyingLocked = false;
                    }

                    if (trackChanges) _changeLog[document.FullName] = new LastChanges(DateTime.Now.Ticks, lineEnding);
                    if (writeReport) Output(string.Format($"{LogStrings.OperationResultTemplate}\n", document.FullName, numberOfIndividualChanges, numberOfAllLineEndings));
                }
                else
                {
                    if (writeReport) Output(string.Format($"{LogStrings.NoModificationRequiredTemplate}\n", document.FullName));
                }
            }

            documentWindow?.Close();
        }

        private void UnifyLineEndingsInDocument(TextDocument textDocument, LineEnding lineEnding, out int? numberOfIndividualChanges, out int? numberOfAllLineEndings, bool writeReport)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var textBuffer = GetTextBuffer(textDocument.Parent.FullName);

            string newlineString = null;

            if (OptionsPage.RemoveTrailingWhitespace)
            {
                var consecutiveWhiteSpaceFollowedByLineEndingFinderFactory = _lineEndingFinderFactoryProvider.GetConsecutiveWhiteSpaceFollowedByLineEndingFinderFactory();
                var consecutiveWhiteSpaceFollowedByLineEndingFinder = consecutiveWhiteSpaceFollowedByLineEndingFinderFactory.Create(textBuffer.CurrentSnapshot);
                var consecutiveWhiteSpaceFollowedByLineEndingMatches = consecutiveWhiteSpaceFollowedByLineEndingFinder.FindAll().ToArray();

                if (consecutiveWhiteSpaceFollowedByLineEndingMatches.Length > 0)
                {
                    newlineString = Utilities.GetNewlineString(lineEnding);

                    using (var textEdit = textBuffer.CreateEdit())
                    {
                        foreach (var consecutiveWhiteSpaceFollowedByLineEndingMatch in consecutiveWhiteSpaceFollowedByLineEndingMatches)
                        {
                            textEdit.Delete(consecutiveWhiteSpaceFollowedByLineEndingMatch);
                        }

                        textEdit.Apply();
                    }
                }
            }

            ChangeLineEndings(_lineEndingFinderFactoryProvider, textBuffer, lineEnding, out numberOfIndividualChanges, out numberOfAllLineEndings, writeReport);

            if (OptionsPage.AddNewlineOnLastLine)
            {
                var documentText = textBuffer.CurrentSnapshot.GetText();
                if (!documentText.EndsWith(Utilities.GetNewlineString(lineEnding)))
                {
                    var editPoint = textDocument.EndPoint.CreateEditPoint();
                    if (editPoint.AtEndOfDocument && !editPoint.AtStartOfLine)
                    {
                        if (newlineString == null)
                        {
                            newlineString = Utilities.GetNewlineString(lineEnding);
                        }

                        editPoint.Insert(newlineString);
                    }
                }
            }
        }

        private ITextBuffer GetTextBuffer(string documentFullPath)
        {
            var editorAdaptersFactoryService = _componentModel.GetService<IVsEditorAdaptersFactoryService>();
            if (editorAdaptersFactoryService == null) throw new COMException($"Unable to resolve service {nameof(IVsEditorAdaptersFactoryService)}");

            if (VsShellUtilities.IsDocumentOpen(this,
                                                documentFullPath,
                                                Guid.Empty,
                                                out _,
                                                out _,
                                                out var windowFrame))
            {
                var view = VsShellUtilities.GetTextView(windowFrame);
                if (view.GetBuffer(out var textLines) == 0)
                {
                    if (textLines is IVsTextBuffer buffer) return editorAdaptersFactoryService.GetDataBuffer(buffer);
                }
            }

            return null;
        }

        private void Output(string message)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            _outputWindow.GetPane(ref _outputWindowGuid, out var outputWindowPane);

            outputWindowPane.OutputStringThreadSafe(message);
        }

        private Document GetDocumentFromDocCookie(uint docCookie)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var documentInfoMoniker = _runningDocumentTable.GetDocumentInfo(docCookie).Moniker;
            //var textBuffer = GetTextBuffer(documentInfoMoniker).Properties;

            var documents = _ide.Documents;
            foreach (Document document in documents)
            {
                if (document.FullName == documentInfoMoniker) return document;
            }

            return null;
        }

        private bool DocumentMatchesConfiguredFileFormatsOrFilenames(string filename) => filename.EndsWithAny(OptionsPage.SupportedFileFormatsArray) || filename.EqualsAny(OptionsPage.SupportedFilenamesArray);
    }
}
