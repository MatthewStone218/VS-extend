using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System.Threading.Tasks;
using System;
using Microsoft.VisualStudio.Threading;

namespace VS_extend
{
    public class VSOutput
    {
        public static JoinableTaskFactory _jtf;
        // 이 메서드는 async Task를 반환하는 메서드 내에서 호출되어야 합니다.
        public static void Message(string message)
        {
            JoinableTask jt = _jtf.RunAsync(async () =>
            {
                try
                {
                    await OutputAndShowPaneAsync(message);
                }
                catch (Exception e)
                {
                    System.Diagnostics.Debug.WriteLine(e.Message);
                }
            });
        }
        public static async Task OutputAndShowPaneAsync(string message)
        {
            // 1. UI 스레드 보장
            // Visual Studio 서비스를 호출하기 전에 항상 UI 스레드로 전환해야 합니다.
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            // 2. 서비스 획득
            // IVsOutputWindow 서비스를 획득합니다.
            IVsOutputWindow outputWindow = await ServiceProvider.GetGlobalServiceAsync(typeof(SVsOutputWindow)) as IVsOutputWindow;

            if (outputWindow == null)
            {
                // 서비스 획득 실패 시 종료
                return;
            }

            Guid generalPaneGuid = Microsoft.VisualStudio.VSConstants.GUID_OutWindowGeneralPane; // 일반 출력 창 GUID
            IVsOutputWindowPane pane;

            // 3. 출력 창 Pane 획득 또는 생성
            // 일반 출력 창(General Pane)을 가져옵니다.
            // GUID가 이미 등록되어 있지 않다면, CreatePane은 자동으로 생성합니다.
            outputWindow.GetPane(ref generalPaneGuid, out pane);

            if (pane == null)
            {
                // 일반 창이 등록되지 않았다면, 새로 생성합니다.
                // 일반 창은 VS에 의해 미리 생성되어 있을 가능성이 높지만 안전을 위해 포함합니다.
                outputWindow.CreatePane(ref generalPaneGuid, "일반", 1 /* fInitVisible */, 0 /* fClearWithSolution */);
                outputWindow.GetPane(ref generalPaneGuid, out pane);
            }

            if (pane == null) return; // 최종 확인

            // 4. 메시지 출력
            // 메시지를 Pane에 씁니다. (자동으로 줄바꿈은 포함되지 않습니다.)
            pane.OutputStringThreadSafe($"[VSIX] {message}\n");

            // 5. 출력 창 활성화 (UI 전환)
            // 출력 창을 활성화하고 현재 표시 중인 UI를 출력 창으로 전환(포커스)합니다.
            pane.Activate();
        }
    }
}
