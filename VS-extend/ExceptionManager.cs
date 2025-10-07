using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EnvDTE;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Threading;
using Newtonsoft.Json.Linq;

namespace VS_extend
{
    public class ExceptionManager
    {
        private JoinableTaskFactory _jtf;
        public CancellationTokenSource CTS;
        public CancellationToken CT;
        private List<Task> TaskList = new List<Task>();
        public ExceptionManager(JoinableTaskFactory jtf)
        {
            _jtf = jtf;
            CreateNewCTS();
            StartObserving();
        }
        public void Cancel()
        {
            CTS.Cancel();
        }
        public void Throw()
        {
            CT.ThrowIfCancellationRequested();
        }
        private void CreateNewCTS()
        {
            CTS = new CancellationTokenSource();
            CT = CTS.Token;
            TaskList.Clear();
        }
        public void Register(Task t)
        {
            TaskList.Add(t);
        }
        private void StartObserving()
        {
            JoinableTask jt = _jtf.RunAsync(async () => await WaitAndCancelAllTasksAsync());
        }
        private async Task WaitAndCancelAllTasksAsync()
        {
            while (true)
            {
                Task finishedTask = await Task.WhenAny(TaskList);
                TaskList.Remove(finishedTask);
                if (finishedTask.IsFaulted)
                {
                    CTS.Cancel();
                }
                if(TaskList.Count == 0)
                {
                    CTS.Dispose();
                    CreateNewCTS();
                }
            }
        }
    }
}
