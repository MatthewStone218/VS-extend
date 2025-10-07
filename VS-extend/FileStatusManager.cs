using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Threading;
using VS_extend.VSExtension;

namespace VS_extend
{
    public class FileStatusManager
    {
        private readonly ErrorListService _errorListService;
        private readonly JoinableTaskFactory _jtf;
        public FileStatusManager(ErrorListService errorListService, JoinableTaskFactory jtf) {
            _errorListService = errorListService;
            _jtf = jtf;
        }
        private Dictionary<string, string> Files = new Dictionary<string, string>{ };//path, error message
        public void SavedFile(string path, bool problem_found, string message)
        {
            if (problem_found)
            {
                Files[path] = message;
            }
            else
            {
                Files.Remove(path);
            }
        }
        public void CleanUpNonExistentFiles(Dictionary<string, string> files)
        {
            // 1. 제거할 키(경로)들을 저장할 리스트를 만듭니다.
            var keysToRemove = new List<string>();

            // 2. 딕셔너리를 반복하면서 실제 파일 존재 여부를 확인합니다.
            foreach (var entry in files)
            {
                string filePath = entry.Key; // 딕셔너리의 키는 파일 경로입니다.

                // 3. File.Exists()를 사용하여 실제 파일이 존재하는지 확인합니다.
                if (!File.Exists(filePath))
                {
                    // 파일이 존재하지 않으면, 제거할 리스트에 키를 추가합니다.
                    keysToRemove.Add(filePath);
                }
            }

            // 4. 반복이 끝난 후, 수집된 키 리스트를 사용하여 딕셔너리에서 항목을 안전하게 제거합니다.
            foreach (string key in keysToRemove)
            {
                files.Remove(key);
                // 필요하다면 제거된 항목에 대한 로깅/디버깅 출력을 추가할 수 있습니다.
                System.Diagnostics.Debug.WriteLine($"존재하지 않는 파일 경로 제거됨: {key}");
            }
        }

        public void ApplyErrorList()
        {
            JoinableTask jt = _jtf.RunAsync(async () => { await _errorListService.ChangeAsync(Files); });
        }
    }
}
