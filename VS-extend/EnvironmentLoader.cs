using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using VS_extend;
using VS_extend.VSExtension;

public class EnvironmentLoader
{
    private VS_extendPackage _VS_extendPackage;
    public EnvironmentLoader(VS_extendPackage __VS_extendPackage)
    {
        _VS_extendPackage = __VS_extendPackage;
    }
    // DTE로 가져온 프로젝트 폴더 경로를 인자로 받습니다.
    public Dictionary<string, string> LoadEnvFile(string projectPath)
    {
        var environmentVariables = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // 프로젝트 경로에 있는 .env 파일의 전체 경로를 구성합니다.
        string envFilePath = Path.Combine(projectPath, ".env");

        // 파일이 존재하는지 확인합니다.
        if (!File.Exists(envFilePath))
        {
            return environmentVariables; // 빈 딕셔너리 반환
        }

        try
        {
            // 파일을 한 줄씩 읽습니다.
            foreach (var line in File.ReadAllLines(envFilePath))
            {
                // 주석(#)이거나 빈 줄은 건너뜁니다.
                if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith("#"))
                {
                    continue;
                }

                // 줄을 '=' 기호를 기준으로 나눕니다.
                int separatorIndex = line.IndexOf('=');

                if (separatorIndex > 0)
                {
                    // 키와 값 추출
                    string key = line.Substring(0, separatorIndex).Trim();
                    string value = line.Substring(separatorIndex + 1).Trim();

                    // 값에서 따옴표(" 또는 ') 제거 (선택 사항)
                    value = value.Trim('"').Trim('\'');

                    // 딕셔너리에 추가합니다.
                    if (!string.IsNullOrEmpty(key))
                    {
                        environmentVariables[key] = value;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            // 파일 읽기 중 발생한 오류 처리
            _VS_extendPackage.main._VSOutput.Message($"VSEXT(EnvironmentLoader.cs) .env 파일을 읽는 중 오류 발생: {ex.Message}");
            _VS_extendPackage._ExceptionManager.Throw();
        }

        return environmentVariables;
    }

    public void CheckAndInitEnvFile(string envFilePath)
    {
        if (!File.Exists(envFilePath))
        {
            File.WriteAllText(envFilePath, "API_KEY=", Encoding.UTF8);
        }
    }
}