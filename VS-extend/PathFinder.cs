using System;
using System.IO;
using EnvDTE;
using Microsoft.VisualStudio.Shell;

public string GetActiveProjectPath(DTE dte)
{
    // UI 스레드에서만 호출될 것을 가정합니다.
    ThreadHelper.ThrowIfNotOnUIThread();

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
        System.Diagnostics.Debug.WriteLine($"프로젝트 경로 획득 오류: {ex.Message}");
    }

    return string.Empty; // 경로를 찾지 못했으면 빈 문자열 반환
}