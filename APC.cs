using System;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.IO;
using System.Web.Script.Serialization;
using System.ComponentModel;

namespace AutoPriorityChanger
{
    namespace Native
    {
        public class Operations
        {
            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern IntPtr OpenProcess(uint access, bool inherit, int id);

            [DllImport("kernel32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool CloseHandle(IntPtr handle);

            [DllImport("ntdll.dll", PreserveSig = false)]
            public static extern void NtSuspendProcess(IntPtr processHandle);

            [DllImport("ntdll.dll", PreserveSig = false)]
            public static extern void NtResumeProcess(IntPtr processHandle);
        }
        /**
         * As seen on https://github.com/cobbr/SharpGen/blob/master/Source/SharpSploit/Execution/Win32.cs, but modified.
         */
        public class ProcessAccessFlags
        {
            // https://msdn.microsoft.com/en-us/library/windows/desktop/ms684880%28v=vs.85%29.aspx?f=255&MSPPError=-2147217396
            public static uint PROCESS_ALL_ACCESS = 0x001F0FFF,
            PROCESS_CREATE_PROCESS = 0x0080,
            PROCESS_CREATE_THREAD = 0x0002,
            PROCESS_DUP_HANDLE = 0x0040,
            PROCESS_QUERY_INFORMATION = 0x0400,
            PROCESS_QUERY_LIMITED_INFORMATION = 0x1000,
            PROCESS_SET_INFORMATION = 0x0200,
            PROCESS_SET_QUOTA = 0x0100,
            PROCESS_SUSPEND_RESUME = 0x0800,
            PROCESS_TERMINATE = 0x0001,
            PROCESS_VM_OPERATION = 0x0008,
            PROCESS_VM_READ = 0x0010,
            PROCESS_VM_WRITE = 0x0020,
            SYNCHRONIZE = 0x00100000;
        }
    }
    public class Utils
    {
        public static T fromDict<T>(Dictionary<String, T> dict, string key)
        {
            foreach (KeyValuePair<String, T> pair in dict)
            {
                if (pair.Key.Equals(key))
                {
                    return pair.Value;
                }
            }
            return default(T);
        }
        private static readonly Dictionary<String, Semaphore> semaphores = new Dictionary<string, Semaphore>();

        public static Semaphore GetSemaphore(String name)
        {
            if (semaphores.ContainsKey(name))
            {
                return semaphores[name];
            }
            var newSemaphore = new Semaphore(1, 1);
            semaphores[name] = newSemaphore;
            return newSemaphore;
        }

    }
    namespace Config
    {

        public class ConfigDTO
        {
            public Dictionary<String, String[]> processgroups;
            public Dictionary<String, ProcessDTO> processes;
            public List<String> exclusions;
            public Int32 timeout;
            public Int32 debug;

        }

        public class ProcessDTO
        {
            public String causesgroupsuspended;
            public String causesgroupidle;
            public String priority;
            public ProcessPriorityClass _priorityClass;
        }

        public class ConfigReader
        {
            private readonly string configFile;

            public ConfigReader(string configFile)
            {
                this.configFile = configFile;
            }

            public ConfigDTO readConfig()
            {
                var json = File.ReadAllText(configFile).ToLower();
                var serializer = new JavaScriptSerializer();
                ConfigDTO dto = serializer.Deserialize<ConfigDTO>(json);
                foreach (KeyValuePair<String, ProcessDTO> pair in dto.processes)
                {
                    var processDTO = pair.Value;
                    var pr = processDTO.priority;
                    if (pr.Contains("ab"))
                    {
                        processDTO._priorityClass = ProcessPriorityClass.AboveNormal;
                    }
                    else if (pr.Contains("be"))
                    {
                        processDTO._priorityClass = ProcessPriorityClass.BelowNormal;
                    }
                    else if (pr.Contains("id"))
                    {
                        processDTO._priorityClass = ProcessPriorityClass.Idle;
                    }
                    else if (pr.Contains("hi"))
                    {
                        processDTO._priorityClass = ProcessPriorityClass.High;
                    }
                    else
                    {
                        processDTO._priorityClass = ProcessPriorityClass.Normal;
                    }
                    processDTO.priority = null;
                }
                return dto;
            }
        }
    }

    namespace Executor
    {
        public class ExecutionContext
        {
            public Process initiator;
            public string[] targets;
            public Semaphore semaphore;
        }

        public class Loop
        {
            private static volatile List<String> extraExclusions;
            private readonly Config.ConfigDTO config;
            private readonly Process[] allProcesses;

            public Loop(Config.ConfigDTO config)
            {
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

            private void debug(String msg)
            {
                if (config.debug == 1)
                {
                    Console.WriteLine(" -- DEBUG --> " + msg);
                }
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
                    debug("                  -> search target: " + name);
                    if (targets.Contains(name))
                    {
                        debug("                  -> found target: " + name);

                        try
                        {
                            originalPriorities.Add(p, p.PriorityClass);
                            p.PriorityClass = ProcessPriorityClass.Idle;
                            extraExclusions.Add(name);
                        }
                        catch (Exception e)
                        {
                            originalPriorities.Remove(p);
                            debug("        -> ignoring process: " + name);
                            debug(e.StackTrace);
                        }
                    }

                }
                initiator.WaitForExit();
                extraExclusions = null;
                foreach (KeyValuePair<Process, ProcessPriorityClass> changed in originalPriorities)
                {
                    changed.Key.PriorityClass = changed.Value;
                }

                config.semaphore.Release();
            }

            private void doSuspend(object configObj)
            {
                ExecutionContext config = configObj as ExecutionContext;
                Process initiator = config.initiator;
                string[] targets = config.targets;
                List<IntPtr> allHandles = new List<IntPtr>();
                Thread.Sleep(5000);

                foreach (Process p in allProcesses)
                {
                    var name = p.ProcessName.ToLower();
                    debug("                  -> search target: " + name);
                    if (targets.Contains(name))
                    {
                        debug("                  -> found target: " + name);
                        IntPtr handle = Native.Operations.OpenProcess(Native.ProcessAccessFlags.PROCESS_SUSPEND_RESUME, false, p.Id);
                        debug("                    -> target handle: " + handle);
                        if (handle != null && handle != IntPtr.Zero && handle.ToInt32() != 0)
                        {
                            allHandles.Add(handle);
                            Native.Operations.NtSuspendProcess(handle);
                        }
                    }
                }
                debug("                  -> waiting");
                initiator.WaitForExit();
                debug("                  -> exited");
                foreach (IntPtr handle in allHandles)
                {
                    Native.Operations.NtResumeProcess(handle);
                    Native.Operations.CloseHandle(handle);
                }
                config.semaphore.Release();
            }

            public void run()
            {
                foreach (Process p in allProcesses)
                {
                    var name = p.ProcessName.ToLower();
                    debug("Process: " + name);
                    var processConfig = Utils.fromDict(config.processes, name);
                    var genericConfig = Utils.fromDict(config.processes, "*");
                    var isException = config.exclusions.Contains(name);
                    if ((
                        processConfig != null ||
                        genericConfig != null
                    ) && !isException)
                    {
                        debug("        -> found!");
                        try
                        {
                            if (processConfig != null)
                            {
                                p.PriorityClass = processConfig._priorityClass;
                            }
                            else
                            {
                                p.PriorityClass = genericConfig._priorityClass;
                            }
                            Thread.Sleep(30);
                        }
                        catch (Exception e)
                        {
                            debug("        -> ignoring process: " + name);
                            debug(e.StackTrace);
                        }
                        debug("        -> priority change");
                        if (processConfig != null)
                        {
                            if (processConfig.causesgroupsuspended != null)
                            {
                                var grp = processConfig.causesgroupsuspended;
                                var theGroup = config.processgroups[grp];
                                var semaphore = Utils.GetSemaphore(grp + "-suspend");
                                if (semaphore.WaitOne(100))
                                {
                                    debug("        -> activate suspend with group: " + grp);
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
                            else if (processConfig.causesgroupidle != null)
                            {
                                var grp = processConfig.causesgroupidle;
                                var theGroup = config.processgroups[grp];
                                var semaphore = Utils.GetSemaphore(grp + "-idle");
                                if (semaphore.WaitOne(100))
                                {
                                    debug("        -> activate idle with group: " + grp);
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
                    }
                }
            }
        }
    }

    public class APC
    {
        public static void Main(string[] args)
        {
            while (true)
            {
                var timeout = 1;
                try
                {
                    var configReader = new Config.ConfigReader("config.json");
                    var config = configReader.readConfig();
                    new Executor.Loop(config).run();
                    timeout = config.timeout;
                    Thread.Sleep(timeout * 1000);
                }
                catch (ArgumentException e)
                {
                    Console.WriteLine(" ==> Invalid Config file! will reload in 300 seconds.");
                    Console.WriteLine(e.StackTrace);
                    Thread.Sleep(300000);
                }
            }
        }
    }
}