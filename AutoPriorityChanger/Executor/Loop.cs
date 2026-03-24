using System;
using System.Threading;
using System.Diagnostics;
using System.Linq;
using System.Collections.Generic;

namespace AutoPriorityChanger.Executor
{
    public class Loop
    {
        private static volatile List<String> extraExclusions;
        private readonly Config.ConfigDTO config;
        private readonly Process[] allProcesses;
        private readonly Utils utils;

        public Loop(Config.ConfigDTO config)
        {
            this.utils = new Utils(config.debug);
            this.config = config;
            if (extraExclusions != null)
            {
                foreach (string exc in extraExclusions)
                {
                    config.exclusions.Add(exc);
                }
            }
            allProcesses = Process.GetProcesses();
        }


        private void doMakeIdle(object configObj)
        {
            ExecutionContext config = configObj as ExecutionContext;
            Process initiator = config.initiator;
            string[] targets = config.targets;
            var originalPriorities = new Dictionary<Process, ProcessPriorityClass>();
            Thread.Sleep(5000);
            extraExclusions = new List<string>();

            foreach (Process p in allProcesses)
            {
                var name = p.ProcessName.ToLower();
                utils.trace("                  -> search target: " + name);
                if (targets.Contains(name))
                {
                    utils.debug("                  -> found target: " + name);

                    try
                    {
                        originalPriorities.Add(p, p.PriorityClass);
                        p.PriorityClass = ProcessPriorityClass.Idle;
                        extraExclusions.Add(name);
                    }
                    catch (Exception e)
                    {
                        originalPriorities.Remove(p);
                        utils.debug("        -> ignoring process: " + name);
                        utils.debug(e.StackTrace);
                    }
                }

            }
            initiator.WaitForExit();
            utils.debug("                  -> exited");
            extraExclusions = null;
            foreach (KeyValuePair<Process, ProcessPriorityClass> changed in originalPriorities)
            {
                try
                {
                    changed.Key.PriorityClass = changed.Value;
                }
                catch (Exception e)
                {
                    Console.WriteLine("ignoring process " + changed.Key.ProcessName);
                    utils.debug(e.StackTrace);
                }
            }

            config.semaphore.Release();
        }

        private void doSuspend(object configObj)
        {
            ExecutionContext config = configObj as ExecutionContext;
            Process initiator = config.initiator;
            string[] targets = config.targets;
            List<Process> allHandles = new List<Process>();
            Thread.Sleep(5000);

            foreach (Process p in allProcesses)
            {
                var name = p.ProcessName.ToLower();
                utils.trace("                  -> search target: " + name);
                if (targets.Contains(name))
                {
                    utils.SuspendProcess(p);
                }
            }
            utils.debug("                  -> waiting");
            initiator.WaitForExit();
            utils.debug("                  -> exited");
            foreach (Process handle in allHandles)
            {
                utils.ResumeProcess(handle);
            }
            config.semaphore.Release();
        }

        public void run()
        {
            foreach (Process p in allProcesses)
            {
                var name = p.ProcessName.ToLower();
                utils.trace("Process: " + name);
                var processConfig = utils.fromDict(config.processes, name);
                var isException = config.exclusions.Contains(name);
                if (processConfig != null && !isException)
                {
                    utils.debug("        -> found!");
                    try
                    {
                        p.PriorityClass = processConfig._priorityClass;
                        Thread.Sleep(30);
                    }
                    catch (Exception e)
                    {
                        utils.debug("        -> ignoring process: " + name);
                        utils.debug(e.StackTrace);
                    }
                    utils.debug("        -> priority change");
                    var theSemaphores = new List<Semaphore>();
                    if (processConfig != null)
                    {
                        if (processConfig.causesgroupsuspended != null)
                        {
                            var grp = processConfig.causesgroupsuspended;
                            var theGroup = config.processgroups[grp];
                            var semaphore = utils.GetSemaphore(grp + "-suspend");
                            if (semaphore.WaitOne(100))
                            {
                                theSemaphores.Add(semaphore);
                                utils.debug("        -> activate suspend with group: " + grp);
                                Thread suspendThread = new Thread(this.doSuspend);
                                var threadCfg = new ExecutionContext()
                                {
                                    initiator = p,
                                    semaphore = semaphore,
                                    targets = theGroup
                                };
                                suspendThread.Start(threadCfg);
                            }
                        }
                        if (processConfig.causesgroupidle != null)
                        {
                            var grp = processConfig.causesgroupidle;
                            var theGroup = config.processgroups[grp];
                            var semaphore = utils.GetSemaphore(grp + "-idle");
                            if (semaphore.WaitOne(100))
                            {
                                theSemaphores.Add(semaphore);
                                utils.debug("        -> activate idle with group: " + grp);
                                Thread idleThread = new Thread(this.doMakeIdle);
                                var threadCfg = new ExecutionContext()
                                {
                                    initiator = p,
                                    semaphore = semaphore,
                                    targets = theGroup
                                };
                                idleThread.Start(threadCfg);
                            }
                        }
                    }
                    foreach (Semaphore s in theSemaphores)
                    {
                        s.WaitOne();
                        s.Release();
                    }
                }
            }
            utils.debug("closing processes...");
            foreach (Process p in allProcesses)
            {
                var id = p.Id;
                utils.trace("closing " + id);
                p.Close();
                utils.trace("closed " + id);
            }
            utils.debug("closed processes!");
        }
    }
}