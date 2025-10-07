using System;
using System.IO;
using System.Threading.Tasks;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;
using VS_extend;
using VS_extend.VSExtension;

public class PathFinder
{
    private readonly JoinableTaskFactory _jtf;
    public PathFinder(JoinableTaskFactory jtf)
    {
        _jtf = jtf;
    }
    // DTE 객체를 받아서 활성 프로젝트의 경로를 반환하는 메서드
    public async Task<string> GetActiveProjectPathAsync(DTE dte)
    {
        VS_extendPackage._VS_extendPackage.main._ExceptionManager.Throw();
        await _jtf.SwitchToMainThreadAsync();

        try
        {
            if (dte.Solution.Projects.Count > 0)
            {
                // 현재 솔루션에서 첫 번째 프로젝트를 가져옵니다.
                // *활성(선택된) 프로젝트* 대신 *솔루션의 프로젝트*를 사용하는 것이
                // VS가 열리는 시점(초기화 시점)에 더 안정적일 수 있습니다.
                Project project = dte.Solution.Projects.Item(1);

                if (project != null && !string.IsNullOrEmpty(project.FullName))
                {
                    // .csproj 파일 경로에서 디렉토리 경로만 추출하여 반환
                    return Path.GetDirectoryName(project.FullName);
                }
            }
        }
        catch (Exception ex)
        {
            VSOutput.Message($"VSEXT(PathFinder.cs) 프로젝트 경로 획득 오류: {ex.Message}");
            VS_extendPackage._VS_extendPackage.main._ExceptionManager.Cancel();
        }

        return string.Empty; // 경로를 찾지 못했으면 빈 문자열 반환
    }
}