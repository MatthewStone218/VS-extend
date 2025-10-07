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
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [ProvideAutoLoad(UIContextGuids80.SolutionExists, PackageAutoLoadFlags.BackgroundLoad)]
    [Guid("705E62DA-DCD2-402B-96DA-4D65A7B6244A")]
    public sealed class VS_extendPackage : AsyncPackage, IVS_extendPackage
    {
        public Main _main;
        public CancellationToken __CancellationToken;
        public IProgress<ServiceProgressData> __Progress;
        public ExceptionManager __ExceptionManager;
        private JoinableTaskFactory __jtf;

        public ExceptionManager _ExceptionManager => __ExceptionManager;
        public JoinableTaskFactory _jtf => __jtf;
        public CancellationToken _CancellationToken => __CancellationToken;
        public IProgress<ServiceProgressData> _Progress => __Progress;
        public Main main { get => _main; set => _main = value; }
        public System.IServiceProvider Provider => this;
        public IAsyncServiceProvider AsyncProvider => this;

        // Package가 로드될 때(초기화) 실행되는 메서드
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await base.InitializeAsync(cancellationToken, progress);
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            __CancellationToken = cancellationToken;
            __Progress = progress;
            __jtf = JoinableTaskFactory;
            __ExceptionManager = new ExceptionManager(this, _jtf);
            main = new Main(this, _CancellationToken, _Progress, _jtf);
            JoinableTask jt = _jtf.RunAsync(async () => await main.InitAsync());
        }
        
        public async Task StartExtensionAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(_CancellationToken);
            __ExceptionManager = new ExceptionManager(this, _jtf);
            main = new Main(this, _CancellationToken, _Progress, _jtf);
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
        // VS_extendPackage.cs 파일 내부에 있는 ShowMessageBox 메서드를 다음으로 교체
    }

    public interface IVS_extendPackage
    {
        Task StartExtensionAsync();
        void StopExtension();
        ExceptionManager _ExceptionManager { get; }
        JoinableTaskFactory _jtf { get; }
        Main main { get; set; }
        Task<object> GetServiceAsync(Type serviceType);
        IServiceProvider Provider { get; }
    }
}