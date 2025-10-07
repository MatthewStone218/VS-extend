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
        public FileStatusManager _FileStatusManager;
        public DocumentEventHandler _DocumentEventHandler;
        public GeminiFeedbackService _GeminiService;
        public Scheduler FileScanScheduler;
        public VSOutput _VSOutput;
        public JoinableTaskFactory _jtf;
        private IVS_extendPackage _IVS_extendPackage;
        public Main(VS_extendPackage __IVS_extendPackage, CancellationToken cancellationToken, IProgress<ServiceProgressData> progress, JoinableTaskFactory jtf)
        {
            _CancellationToken = cancellationToken;
            _Progress = progress;
            _IVS_extendPackage = __IVS_extendPackage;
            _jtf = jtf;
        }
        
        public async Task InitAsync()
        {
            // 1. UI 스레드 전환 요청
            // DTE 서비스에 접근하려면 반드시 UI 스레드(Main Thread)로 전환해야 합니다.
            await _jtf.SwitchToMainThreadAsync(_CancellationToken);

            // 2. DTE 서비스 가져오기
            DTE _DTE = await _IVS_extendPackage.GetServiceAsync(typeof(DTE)) as DTE;
            
            if (_DTE == null) return;

            // 3. 현재 프로젝트 경로를 가져와 저장
            PathFinder pathFinder = new PathFinder(_IVS_extendPackage, _jtf);
            EnvironmentLoader environmentLoader = new EnvironmentLoader(_IVS_extendPackage);
            ErrorListService errorListService = new ErrorListService(_IVS_extendPackage, _jtf);
            _FileStatusManager = new FileStatusManager(_IVS_extendPackage, errorListService, _jtf);
            _DocumentEventHandler = new DocumentEventHandler(_IVS_extendPackage, _jtf);
            _VSOutput = new VSOutput(_IVS_extendPackage, _jtf);

            string ProjectPath = await pathFinder.GetActiveProjectPathAsync(_DTE);
            environmentLoader.CheckAndInitEnvFile(Path.Combine(ProjectPath, ".env"));
            Dictionary<string, string> variable = environmentLoader.LoadEnvFile(Path.Combine(ProjectPath, ".env"));
            string APIKey = variable.TryGetValue("API_KEY", out string apiKey) ? apiKey : null;
            if (APIKey == null || APIKey == "") return;
            _GeminiService = new GeminiFeedbackService(_IVS_extendPackage, APIKey);

            _DocumentEventHandler.Init();

            _DocumentEventHandler.CallbackAfterSave = (args) =>
            {
                if (args.TryGetValue("fileContent", out object fileContentObj) && fileContentObj is string fileContent && args.TryGetValue("filePath", out object filePathObject) && filePathObject is string filePath)
                {

                    JoinableTask jt = _jtf.RunAsync(async () => {
                        try
                        {
                            _IVS_extendPackage._ExceptionManager.Throw();
                            var response = await _GeminiService.GetFeedbackAsync(fileContent);
                            bool problemFound = response.ProblemFound;
                            string message = response.Message;
                            _FileStatusManager.SavedFile(filePath, problemFound, message);
                        }
                        catch (Exception e)
                        {
                            _IVS_extendPackage.main._VSOutput.Message($"VSEXT(Main.cs) 파일 저장에 따른 API호출에 문제가 발생했습니다. {e}");
                            _IVS_extendPackage._ExceptionManager.Throw();
                        }
                    });
                    _IVS_extendPackage._ExceptionManager.Register(jt.Task);
                }
            };
            FileScanScheduler = new Scheduler(() => {
                try
                {
                    _FileStatusManager.CleanUpNonExistentFiles();
                }
                catch (Exception e)
                {
                    _IVS_extendPackage.main._VSOutput.Message($"VSEXT(Main.cs) 삭제된 파일을 스캔하는 과정에서 예외가 발생했습니다. {e}");
                    _IVS_extendPackage._ExceptionManager.Throw();
                }
            }, null, 0, 3000);
            FileScanScheduler.StartTask();
        }

        public async Task StopAsync()
        {
            await DisposeAsync();
        }

        public async Task DisposeAsync()
        {
            _FileStatusManager?.Dispose();
            _GeminiService?.Dispose();
            FileScanScheduler?.Dispose();
            await _DocumentEventHandler?.DisposeAsync();
        }
    }
}
