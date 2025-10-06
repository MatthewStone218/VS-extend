using System;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using VS_extend.VSExtension;
using Task = System.Threading.Tasks.Task;

namespace VS_extend.VSExtension // 네임스페이스 일치
{
    // VS Package Guid 등 기존 속성은 그대로 둡니다.
    // ...
    [ProvideAutoLoad(UIContextGuids80.SolutionExists)] // 솔루션이 열릴 때 자동 로드
    [Guid("705E62DA-DCD2-402B-96DA-4D65A7B6244A")]
    public sealed class VS_extendPackage : AsyncPackage
    {
        private ErrorListService _errorListService;

        // Package가 로드될 때(초기화) 실행되는 메서드
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            _errorListService = new ErrorListService(this);
            _errorListService.InitializeMessage("⭐ 문제가 발견되지 않았습니다. ⭐");
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