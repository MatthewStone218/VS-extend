using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;
using VS_extend;
using VS_extend.VSExtension;
using Task = System.Threading.Tasks.Task;

// IVsRunningDocTableEvents 인터페이스를 구현합니다.
public class DocumentEventHandler : IVsRunningDocTableEvents
{
    private readonly JoinableTaskFactory _jtf;
    private IVsRunningDocumentTable _rdt;
    private uint _eventCookie;
    private readonly System.IServiceProvider _serviceProvider;
    private JoinableTask InitTask;
    public Action<Dictionary<string, object>> CallbackAfterSave { get; set; }

    public DocumentEventHandler(System.IServiceProvider serviceProvider, JoinableTaskFactory jtf)
    {
        _serviceProvider = serviceProvider;
        _jtf = jtf;
        InitTask = _jtf.RunAsync(async () => await InitAsync());
        VS_extendPackage._VS_extendPackage.main._ExceptionManager.Register(InitTask.Task);
    }
    private async Task InitAsync()
    {
        await _jtf.SwitchToMainThreadAsync();

        // RDT 서비스 가져오기
        _rdt = _serviceProvider.GetService(typeof(SVsRunningDocumentTable)) as IVsRunningDocumentTable;

        if (_rdt != null)
        {
            // 이벤트를 RDT에 등록하고 쿠키(식별자)를 저장합니다.
            _rdt.AdviseRunningDocTableEvents(this, out _eventCookie);
        }
    }
    // 문서가 저장된 후 호출되는 핵심 메서드
    public int OnAfterSave(uint docCookie)
    {
        JoinableTask jt = _jtf.RunAsync(async () => await OnAfterSaveAsync(docCookie));
        VS_extendPackage._VS_extendPackage.main._ExceptionManager.Register(jt.Task);
        return VSConstants.S_OK;
    }
    public async Task<int> OnAfterSaveAsync(uint docCookie)
    {
        await _jtf.SwitchToMainThreadAsync();

        // docCookie를 사용하여 저장된 문서 정보(파일명, 경로 등)를 가져옵니다.
        IVsHierarchy pHier;
        uint itemId;
        IntPtr pData;

        // RDT에서 문서 정보를 검색합니다.
        _rdt.GetDocumentInfo(
            docCookie,
            out uint pFlags,
            out uint pReadLocks,
            out uint pEditLocks,
            out string pbstrMkDocument, // <-- 파일 경로(Full Path)
            out pHier,
            out itemId,
            out pData);

        // 여기서 원하는 코드를 실행합니다.
        if (!string.IsNullOrEmpty(pbstrMkDocument))
        {
            try
            {
                string fileContent = File.ReadAllText(pbstrMkDocument);
                Dictionary<string, object> args = new Dictionary<string, object>
                {
                    { "fileContent", fileContent },
                    { "filePath", pbstrMkDocument }
                };
                CallbackAfterSave(args);
            }
            catch (Exception e)
            {
                VSOutput.Message($"VSEXT(DocumentEventHandler.cs) 파일을 읽는 도중 문제 발생. {e}");
                VS_extendPackage._VS_extendPackage.main._ExceptionManager.Cancel();
            }
        }

        return VSConstants.S_OK;
    }

    // 이 외의 IVsRunningDocTableEvents 메서드는 기본적으로 S_OK를 반환합니다.
    public int OnBeforeLastDocumentUnlock(uint docCookie, uint dwRDTLockType, uint dwReadLocksRemaining, uint dwEditLocksRemaining) { return VSConstants.S_OK; }
    public int OnAfterFirstDocumentLock(uint docCookie, uint dwRDTLockType, uint dwReadLocksRemaining, uint dwEditLocksRemaining) { return VSConstants.S_OK; }
    public int OnBeforeDocumentWindowShow(uint docCookie, int fFirstShow, IVsWindowFrame pFrame) { return VSConstants.S_OK; }
    public int OnAfterDocumentWindowHide(uint docCookie, IVsWindowFrame pFrame) { return VSConstants.S_OK; }
    public int OnBeforeSave(uint docCookie) { return VSConstants.S_OK; } // 저장이 시작되기 직전에 실행할 로직이 있다면 여기에
    public int OnAfterAttributeChange(uint docCookie, uint grfAttribs) { return VSConstants.S_OK; }
    public int OnAfterAttributeChangeEx(uint docCookie, uint grfAttribs, IVsHierarchy pHierOld, uint itemidOld, string pszMkDocumentOld, IVsHierarchy pHierNew, uint itemidNew, string pszMkDocumentNew) { return VSConstants.S_OK; }
    public int OnAfterDocumentClose(uint docCookie) { return VSConstants.S_OK; }

    // RDT 이벤트 등록 해제 (리소스 정리)
    public async Task DisposeAsync()
    {
        await InitTask;
        if (_eventCookie != 0)
        {
            _rdt.UnadviseRunningDocTableEvents(_eventCookie);
            _eventCookie = 0;
        }
    }
}