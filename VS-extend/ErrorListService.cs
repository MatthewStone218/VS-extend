using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using Microsoft.VisualStudio;

namespace VS_extend.VSExtension
{
    public class ErrorListService : IDisposable
    {
        private readonly ErrorListProvider _errorListProvider;
        // ⭐ 이 변수에 우리가 관리할 하나의 ErrorTask 객체를 저장합니다.
        private ErrorTask _persistentTask;
        private readonly IServiceProvider _serviceProvider;

        public ErrorListService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _errorListProvider = new ErrorListProvider(serviceProvider);
        }

        // 메시지를 생성하고 등록하는 초기 메서드 (확장 프로그램 시작 시 한 번 호출)
        public void InitializeMessage(string initialMessage)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            // Task 객체 생성
            _persistentTask = new ErrorTask
            {
                Text = initialMessage,
                ErrorCategory = TaskErrorCategory.Message,
                Line = 0,
                Column = 0,
                Category = TaskCategory.Misc,
            };

            // Provider에 Task를 추가
            _errorListProvider.Tasks.Add(_persistentTask);
            _errorListProvider.Show();
        }

        // ⭐ 핸들을 사용하여 기존 메시지의 내용만 업데이트하는 메서드
        public void UpdateMessageText(string newMessage)
        {
            // UI 스레드 확인 (VSIX 개발 시 필수)
            ThreadHelper.ThrowIfNotOnUIThread();

            if (_persistentTask == null)
            {
                InitializeMessage(newMessage);
                return;
            }

            // 1. 기존 Task 객체의 내용(Text 속성)만 변경합니다.
            _persistentTask.Text = newMessage;

            // 2. TaskProvider가 변경 사항을 확인하고 UI를 갱신하도록 요청합니다.
            // 이 방법이 OnTaskChanged를 사용할 수 없을 때 가장 안정적인 대체 방법입니다.
            _errorListProvider.Refresh();
        }

        public void Dispose()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            // 확장 종료 시 Task를 제거하고 Provider 리소스를 정리합니다.
            if (_persistentTask != null)
            {
                _errorListProvider.Tasks.Remove(_persistentTask);
            }
            _errorListProvider?.Dispose();
        }
    }
}