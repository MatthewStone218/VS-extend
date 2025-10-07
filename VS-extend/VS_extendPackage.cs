using System;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using VS_extend.VSExtension;
using Task = System.Threading.Tasks.Task;
using System.IO;
using EnvDTE; // DTE를 사용하기 위해 필요
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.VisualStudio.Threading;

namespace VS_extend.VSExtension // 네임스페이스 일치
{
    [ProvideAutoLoad(UIContextGuids80.SolutionExists, PackageAutoLoadFlags.BackgroundLoad)] // 솔루션이 열릴 때 자동 로드
    [Guid("705E62DA-DCD2-402B-96DA-4D65A7B6244A")]
    public sealed class VS_extendPackage : AsyncPackage
    {
        public string ProjectPath = null;
        public DTE _DTE = null;
        public string APIKey = null;
        private DocumentEventHandler _documentEventHandler;

        // Package가 로드될 때(초기화) 실행되는 메서드
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            Main main = new Main(cancellationToken, progress, this);
            await main.InitAsync();
        }

        // Package가 언로드될 때 리소스를 정리합니다.
        protected override void Dispose(bool disposing)
        {
            // ThreadHelper를 사용하여 UI 스레드에서 Dispose가 실행되도록 보장합니다.
            ThreadHelper.ThrowIfNotOnUIThread();
            try
            {
                if (disposing)
                {
                    // Managed Resources (관리되는 리소스, 즉 C# 객체들)를 정리합니다.

                    // 1. DocumentSaveHandler의 Dispose 메서드를 호출하여 RDT 등록을 해제합니다.
                    if (_documentEventHandler != null)
                    {
                        // RDT Unadvise는 UI 스레드에서만 가능합니다.
                        _documentEventHandler.Dispose();
                        _documentEventHandler = null;
                    }

                    // 2. 다른 IDisposable 객체들을 여기서 정리합니다.
                }
            }
            finally
            {
                // Unmanaged Resources (비관리 리소스) 정리가 있다면 여기에 넣습니다.
                // 기본 클래스의 Dispose를 반드시 호출해야 합니다.
                base.Dispose(disposing);
            }
        }
    }
}