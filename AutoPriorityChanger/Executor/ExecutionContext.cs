using System.Threading;
using System.Diagnostics;

namespace AutoPriorityChanger.Executor
{
    public class ExecutionContext
    {
        public Process initiator;
        public string[] targets;
        public Semaphore semaphore;
    }
}