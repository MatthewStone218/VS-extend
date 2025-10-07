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
        public List<Task> TaskList;
        public ExceptionManager(JoinableTaskFactory jtf)
        {
            _jtf = jtf;
            CTS = new CancellationTokenSource();
            CT = CTS.Token;
            TaskList = new List<Task>();
            StartObserving();
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
