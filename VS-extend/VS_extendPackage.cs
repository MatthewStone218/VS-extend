using System;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using VS_extend.VSExtension;
using Task = System.Threading.Tasks.Task;
using System.IO;
using EnvDTE; // DTE를 사용하기 위해 필요
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.VisualStudio.Threading;

namespace VS_extend.VSExtension // 네임스페이스 일치
{
    [ProvideAutoLoad(UIContextGuids80.SolutionExists)] // 솔루션이 열릴 때 자동 로드
    [Guid("705E62DA-DCD2-402B-96DA-4D65A7B6244A")]
    public sealed class VS_extendPackage : AsyncPackage
    {
        public string ProjectPath = null;
        public DTE _DTE = null;
        public string APIKey = null;
        private DocumentSaveHandler _saveHandler;

        // Package가 로드될 때(초기화) 실행되는 메서드
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            // 1. UI 스레드 전환 요청
            // DTE 서비스에 접근하려면 반드시 UI 스레드(Main Thread)로 전환해야 합니다.
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            // 2. DTE 서비스 가져오기
            DTE _DTE = await GetServiceAsync(typeof(DTE)) as DTE;

            if (_DTE == null) return;

            // 3. 현재 프로젝트 경로를 가져와 저장
            string ProjectPath = PathFinder.GetActiveProjectPath(_DTE);
            Dictionary<string,string> variable = EnvironmentLoader.LoadEnvFile(Path.Combine(ProjectPath, ".env"));
            APIKey = variable.TryGetValue("API_KEY", out string apiKey) ? apiKey : null;

            ErrorListService errorListService = new ErrorListService(this);
            FileStatusManager fileStatusManager = new FileStatusManager(errorListService);

            _saveHandler = new DocumentSaveHandler(this);
            _saveHandler.CallbackAfterSave = (args) =>
            {
                if (APIKey == null) return;
                if (args.TryGetValue("fileContent", out object fileContentObj) && fileContentObj is string fileContent)
                {
                    GeminiFeedbackService geminiService = new GeminiFeedbackService(APIKey);
                    Task.Run(async () => { 
                        var response = await geminiService.GetFeedbackAsync(fileContent);
                        string message = response.ProblemFound
                        ? $"[VS_extend] OS 종속적인 코드가 발견되었습니다: {response.Message}"
                        : "[VS_extend] 아무 문제도 발견되지 않았습니다.";
                    }).Forget();
                }
            };
        }

        // Package가 언로드될 때 리소스를 정리합니다.
        protected override void Dispose(bool disposing)
        {
            try
            {
                if (disposing)
                {
                    // Managed Resources (관리되는 리소스, 즉 C# 객체들)를 정리합니다.

                    // 1. DocumentSaveHandler의 Dispose 메서드를 호출하여 RDT 등록을 해제합니다.
                    if (_saveHandler != null)
                    {
                        // ThreadHelper를 사용하여 UI 스레드에서 Dispose가 실행되도록 보장합니다.
                        // RDT Unadvise는 UI 스레드에서만 가능합니다.
                        ThreadHelper.ThrowIfNotOnUIThread();
                        _saveHandler.Dispose();
                        _saveHandler = null;
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