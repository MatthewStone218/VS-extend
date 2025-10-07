using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;
using VS_extend.VSExtension;

public class ErrorListService : IDisposable
{
    // ErrorListProvider 인스턴스는 한 번만 생성하여 재사용합니다.
    private readonly ErrorListProvider _errorProvider;
    private readonly JoinableTaskFactory _jtf;

    public ErrorListService(IServiceProvider serviceProvider, JoinableTaskFactory jtf)
    {
        // ErrorListProvider 초기화
        // 서비스 프로바이더는 보통 Package 클래스 인스턴스입니다.
        _errorProvider = new ErrorListProvider(serviceProvider);
        _jtf = jtf;
    }

    /// <summary>
    /// 새로운 사용자 정의 오류 목록을 표시하고, 기존 항목은 제거합니다.
    /// 이 메서드는 Dictionary<파일 경로, 오류 메시지>를 인자로 받습니다.
    /// </summary>
    /// <param name="errors">키: 파일 경로, 값: 오류 메시지</param>
    public async Task ChangeAsync(Dictionary<string, string> errors)
    {
        VS_extendPackage._VS_extendPackage.main._ExceptionManager.Throw();
        await _jtf.SwitchToMainThreadAsync();

        // 기존에 이 Provider가 등록했던 모든 오류 항목을 지웁니다.
        _errorProvider.Tasks.Clear();

        if (errors == null || errors.Count == 0)
        {
            // 오류가 없으면 목록을 비우고 끝냅니다.
            _errorProvider.Refresh();
        }

        foreach (var errorEntry in errors)
        {
            string filePath = errorEntry.Key;
            string message = errorEntry.Value;

            // 새로운 오류 항목 (ErrorTask) 생성
            ErrorTask errorTask = new ErrorTask()
            {
                Text = message, // 오류 메시지
                Category = TaskCategory.User, // 사용자 정의 항목으로 분류
                Priority = TaskPriority.High, // 우선순위: High (목록 상단에 표시될 가능성 높음)
                // ErrorType을 Error로 설정하여 VS 오류와 동일하게 표시되도록 합니다.
                ErrorCategory = TaskErrorCategory.Error,

                // 파일 정보 설정 (더블 클릭 시 해당 파일로 이동 가능)
                Document = filePath,
                // Line은 0부터 시작하는 줄 번호입니다. 
                // 특정 위치가 없으므로 0으로 설정 (파일의 첫 줄을 가리킴)
                Line = 0
            };

            // 항목 더블 클릭 시 파일 열기 로직 설정
            errorTask.Navigate += ErrorTask_Navigate;

            // Provider에 항목을 추가합니다.
            _errorProvider.Tasks.Add(errorTask);
        }

        // 오류 목록 창을 강제로 업데이트합니다.
        _errorProvider.Refresh();
    }

    // ErrorTask가 더블 클릭되었을 때 호출되는 이벤트 핸들러
    private void ErrorTask_Navigate(object sender, EventArgs arguments)
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        ErrorTask task = sender as ErrorTask;
        if (task != null && task.Document != null)
        {
            // 파일을 열고 해당 줄(Line)로 이동합니다.
            _errorProvider.Navigate(task, new Guid(EnvDTE.Constants.vsViewKindCode));
        }
    }

    // IDisposable 구현 (리소스 정리)
    public void Dispose()
    {
        if (_errorProvider != null)
        {
            // ErrorListProvider가 사용한 리소스를 해제합니다.
            _errorProvider.Dispose();
        }
    }
}