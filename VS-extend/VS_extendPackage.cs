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

namespace VS_extend.VSExtension // 네임스페이스 일치
{
    [ProvideAutoLoad(UIContextGuids80.SolutionExists)] // 솔루션이 열릴 때 자동 로드
    [Guid("705E62DA-DCD2-402B-96DA-4D65A7B6244A")]
    public sealed class VS_extendPackage : AsyncPackage
    {
        public string ProjectPath = null;
        public DTE _DTE = null;
        public string APIKey = null;
        private ErrorListService _errorListService;

        // Package가 로드될 때(초기화) 실행되는 메서드
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            // 1. UI 스레드 전환 요청
            // DTE 서비스에 접근하려면 반드시 UI 스레드(Main Thread)로 전환해야 합니다.
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            // 2. DTE 서비스 가져오기
            DTE _DTE = await GetServiceAsync(typeof(DTE)) as DTE;

            // 3. 현재 프로젝트 경로를 가져와 저장
            if (_DTE != null)
            {
                string ProjectPath = PathFinder.GetActiveProjectPath(_DTE);

                Dictionary<string,string> variable = EnvironmentLoader.LoadEnvFile(Path.Combine(ProjectPath, ".env"));

                // TODO: 이 시점에서 GeminiFeedbackService를 초기화하고 API 키를 전달할 수 있습니다.
                // var geminiService = new GeminiFeedbackService("YOUR_API_KEY");
            }
        }

        // Package가 언로드될 때 리소스를 정리합니다.
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _errorListService?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}