namespace LineEndingsUnifier
{
    using Microsoft.VisualStudio;
    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Shell.Interop;

    using System;

    internal class DocumentSaveListener : IVsRunningDocTableEvents3, IDisposable
    {
        public event OnBeforeSaveHandler BeforeSave;

        public delegate int OnBeforeSaveHandler(uint docCookie);

        private readonly RunningDocumentTable _runningDocumentTable;
        private readonly uint _cookie;

        public DocumentSaveListener(RunningDocumentTable docTable)
        {
            _runningDocumentTable = docTable;
            _cookie = _runningDocumentTable.Advise(this);
        }

        public int OnAfterFirstDocumentLock(uint docCookie, uint dwRDTLockType, uint dwReadLocksRemaining, uint dwEditLocksRemaining)
        {
            return VSConstants.S_OK;
        }

        public int OnBeforeLastDocumentUnlock(uint docCookie, uint dwRDTLockType, uint dwReadLocksRemaining, uint dwEditLocksRemaining)
        {
            return VSConstants.S_OK;
        }

        public int OnAfterSave(uint docCookie)
        {
            return VSConstants.S_OK;
        }

        public int OnAfterAttributeChange(uint docCookie, uint grfAttribs)
        {
            return VSConstants.S_OK;
        }

        public int OnBeforeDocumentWindowShow(uint docCookie, int fFirstShow, IVsWindowFrame pFrame)
        {
            return VSConstants.S_OK;
        }

        public int OnAfterDocumentWindowHide(uint docCookie, IVsWindowFrame pFrame)
        {
            return VSConstants.S_OK;
        }

        public int OnAfterAttributeChangeEx(uint docCookie, uint grfAttribs, IVsHierarchy pHierOld, uint itemidOld, string pszMkDocumentOld, IVsHierarchy pHierNew, uint itemidNew, string pszMkDocumentNew)
        {
            return VSConstants.S_OK;
        }

        public int OnBeforeSave(uint docCookie)
        {
            if (BeforeSave != null)
            {
                return BeforeSave(docCookie);
            }

            return VSConstants.S_OK;
        }

        public void Dispose()
        {
            _runningDocumentTable.Unadvise(_cookie);
        }
    }
}
