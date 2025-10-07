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

namespace VS_extend
{
    public class Main
    {
        public CancellationToken _CancellationToken;
        public IProgress<ServiceProgressData> _Progress;
        public VS_extendPackage _VsExtendPackage;
        public FileStatusManager _FileStatusManager;
        public DocumentEventHandler _DocumentEventHandler;
        public GeminiFeedbackService _GeminiService;
        public Scheduler FileScanScheduler;
        public Main(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress, VS_extendPackage vsExtendPackage)
        {
            _CancellationToken = cancellationToken;
            _Progress = progress;
            _VsExtendPackage = vsExtendPackage;
        }

        public async Task InitAsync()
        {
            // 1. UI 스레드 전환 요청
            // DTE 서비스에 접근하려면 반드시 UI 스레드(Main Thread)로 전환해야 합니다.
            JoinableTaskFactory jtf = _VsExtendPackage.JoinableTaskFactory;
            await jtf.SwitchToMainThreadAsync(_CancellationToken);

            VSOutput._jtf = jtf;

            // 2. DTE 서비스 가져오기
            DTE _DTE = await _VsExtendPackage.GetServiceAsync(typeof(DTE)) as DTE;

            if (_DTE == null) return;

            // 3. 현재 프로젝트 경로를 가져와 저장
            PathFinder pathFinder = new PathFinder(jtf);
            string ProjectPath = await pathFinder.GetActiveProjectPathAsync(_DTE);
            EnvironmentLoader.CheckAndInitEnvFile(Path.Combine(ProjectPath, ".env"));
            Dictionary<string, string> variable = EnvironmentLoader.LoadEnvFile(Path.Combine(ProjectPath, ".env"));
            string APIKey = variable.TryGetValue("API_KEY", out string apiKey) ? apiKey : null;
            if (APIKey == null || APIKey == "") return;

            ErrorListService errorListService = new ErrorListService(_VsExtendPackage, jtf);
            _FileStatusManager = new FileStatusManager(errorListService, jtf);

            _DocumentEventHandler = new DocumentEventHandler(_VsExtendPackage, jtf);
            _DocumentEventHandler.CallbackAfterSave = (args) =>
            {
                if (args.TryGetValue("fileContent", out object fileContentObj) && fileContentObj is string fileContent && args.TryGetValue("filePath", out object filePathObject) && filePathObject is string filePath)
                {
                    _GeminiService = new GeminiFeedbackService(APIKey);
                    JoinableTask jt = jtf.RunAsync(async () => {
                        try
                        {
                            var response = await _GeminiService.GetFeedbackAsync(fileContent);
                            bool problemFound = response.ProblemFound;
                            string message = response.Message;
                            _FileStatusManager.SavedFile(filePath, problemFound, message);
                        }
                        catch (Exception e)
                        {
                            VSOutput.Message($"파일 저장에 따른 API호출에 문제가 발생했습니다. {e}");
                        }
                    });
                }
            };
            FileScanScheduler = new Scheduler(() => {
                try
                {
                    _FileStatusManager.CleanUpNonExistentFiles();
                }
                catch (Exception e)
                {
                    VSOutput.Message($"삭제된 파일을 스캔하는 과정에서 예외가 발생했습니다. {e}");
                }
            }, null, 0, 3000);
        }

        public async Task StopAsync()
        {
            await DisposeAsync();
        }

        public async Task DisposeAsync()
        {
            _FileStatusManager?.Dispose();
            await _DocumentEventHandler?.DisposeAsync();
            _GeminiService?.Dispose();
            FileScanScheduler?.Dispose();
        }
    }
}
