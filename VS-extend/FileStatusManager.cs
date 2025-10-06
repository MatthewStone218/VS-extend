using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VS_extend.VSExtension;

namespace VS_extend
{
    public class FileStatusManager
    {
        private readonly ErrorListService _errorListService;
        public FileStatusManager(ErrorListService errorListService) {
            _errorListService = errorListService;
        }
        static private Dictionary<string, string> Files = new Dictionary<string, string>{ };//path, error message
        static public void SavedFile(string path, bool problem_found, string message)
        {
            if (problem_found)
            {
                Files.Add(path, message);
            } else
            {
                Files.Remove(path);
            }
        }
        static public void DeletedFile(string path)
        {
            if (Files.TryGetValue(path,out string str))
            {
                Files.Remove(path);
            }
        }
        static public void ApplyErrorList()
        {
            for (int i = 0; i < FileStatusManager.Files.Count; i++)
            { 
                
            }
        }
    }
}
