using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using EnvDTE; // DTE를 사용하기 위해 필요
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;
using VS_extend.VSExtension;
using Task = System.Threading.Tasks.Task;

namespace VS_extend.VSExtension // 네임스페이스 일치
{
    [ProvideAutoLoad(UIContextGuids80.SolutionExists, PackageAutoLoadFlags.BackgroundLoad)] // 솔루션이 열릴 때 자동 로드
    [Guid("705E62DA-DCD2-402B-96DA-4D65A7B6244A")]
    public sealed class VS_extendPackage : AsyncPackage
    {
        static public VS_extendPackage _VS_extendPackage;
        public Main main = null;
        public CancellationToken _CancellationToken;
        public IProgress<ServiceProgressData> _Progress;
        public Main M => main;
        private JoinableTaskFactory _jtf;

        // Package가 로드될 때(초기화) 실행되는 메서드
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            _VS_extendPackage = this;
            _CancellationToken = cancellationToken;
            _Progress = progress;
            _jtf = JoinableTaskFactory;
            StartExtension();
            await Task.Yield();
        }

        public void StartExtension()
        {
            main = new Main(_CancellationToken, _Progress, this, _jtf);
            JoinableTask jt = _jtf.RunAsync(async () => await main.InitAsync());
        }

        public void StopExtension()
        {
            if (main != null)
            {
                JoinableTask jt = _jtf.RunAsync(async () => await main.StopAsync());
                main = null;
            }
        }

        // Package가 언로드될 때 리소스를 정리합니다.
        protected override void Dispose(bool disposing)
        {
            if(main != null)
            {
                _jtf.Run(() => main.StopAsync());
                main = null;
            }
        }
    }
}