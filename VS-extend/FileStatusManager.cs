using System;
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
        public void DeletedFile(string path)
        {
            if (Files.TryGetValue(path,out string str))
            {
                Files.Remove(path);
            }
        }
        public void ApplyErrorList()
        {
            JoinableTask jt = _jtf.RunAsync(async () => { await _errorListService.ChangeAsync(Files); });
        }
    }
}
