using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EnvDTE;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Threading;

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
        private void CreateNewCTS()
        {
            CTS = new CancellationTokenSource();
            CT = CTS.Token;
            TaskList.Clear();
        }
        public void AddTask(Task t)
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
                    CTS.Dispose();
                    CTS = new CancellationTokenSource();
                    CT = CTS.Token;
                    TaskList.Clear();
                }
            }
        }
    }
}
