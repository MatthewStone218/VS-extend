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
            ApplyErrorList();
        }
        public void CleanUpNonExistentFiles()
        {
            var keysToRemove = new List<string>();
            foreach (var entry in Files)
            {
                string filePath = entry.Key;
                if (!File.Exists(filePath))
                {
                    keysToRemove.Add(filePath);
                }
            }

            foreach (string key in keysToRemove)
            {
                Files.Remove(key);
            }

            if (keysToRemove.Count > 0)
            {
                ApplyErrorList();
            }
        }

        public void ApplyErrorList()
        {
            JoinableTask jt = _jtf.RunAsync(async () =>
            {
                try { await _errorListService.ChangeAsync(Files); }
                catch (Exception e)
                {
                    VSOutput.Message($"VSEXT(FileStatusManager.cs) 오류 목록을 작성하던 도중에 예외 발생 {e}");
                    VS_extendPackage._VS_extendPackage.main._ExceptionManager.Throw();
                }
            }
            );
        }
        public void Dispose()
        {
            _errorListService.Dispose();
        }
    }
}
