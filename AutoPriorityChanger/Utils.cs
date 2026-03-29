using System;
using System.Collections.Generic;
using System.Threading;
using System.Diagnostics;
using System.IO;
using AutoPriorityChanger.Native;

namespace AutoPriorityChanger
{
    public class Utils
    {
        public static readonly uint PROCESS_SUSPEND_RESUME = 0x0800;
        private static readonly Dictionary<String, Semaphore> semaphores = new Dictionary<string, Semaphore>();

        private readonly int logDepth = 0;

        static Utils()
        {
            if (File.Exists("apc.log"))
            {
                File.Delete("apc.log");
            }
            StreamWriter wr = File.CreateText("apc.log");
            wr.AutoFlush = true;
            Console.SetOut(wr);
            Console.SetError(wr);
        }

        public Utils(int logDepth)
        {
            this.logDepth = logDepth;
        }

        public void debug(String msg)
        {
            if (logDepth >= 1)
            {
                Console.WriteLine(" -- DEBUG --> " + msg);
            }
        }

        public void trace(String msg)
        {
            if (logDepth >= 2)
            {
                Console.WriteLine(" -- TRACE --> " + msg);
            }
        }
        public T fromDict<T>(Dictionary<String, T> dict, string key)
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

        public Semaphore GetSemaphore(String name)
        {
            if (semaphores.ContainsKey(name))
            {
                return semaphores[name];
            }
            var newSemaphore = new Semaphore(1, 1);
            semaphores[name] = newSemaphore;
            return newSemaphore;
        }

        public void SuspendProcess(Process p)
        {
            IntPtr handle = IntPtr.Zero;
            try
            {
                handle = Operations.OpenProcess(PROCESS_SUSPEND_RESUME, false, p.Id);
                trace("                    -> target handle: " + handle.ToInt64());
                if (handle != IntPtr.Zero)
                {
                    Operations.NtSuspendProcess(handle);
                }
            }
            catch (Exception e)
            {
                debug("                    -> Process will be ignored:" + e.Message);
            }
            finally
            {
                if (handle != IntPtr.Zero)
                {
                    Operations.CloseHandle(handle);
                }
            }
        }

        public void ResumeProcess(Process p)
        {
            IntPtr handle = IntPtr.Zero;
            try
            {
                handle = Operations.OpenProcess(PROCESS_SUSPEND_RESUME, false, p.Id);
                trace("                    -> target handle: " + handle.ToInt64());

                if (handle != IntPtr.Zero)
                {
                    Operations.NtResumeProcess(handle);
                }
            }
            catch (Exception e)
            {
                debug("                    -> Process will be ignored:" + e.Message);
            }
            finally
            {
                if (handle != IntPtr.Zero)
                {
                    Operations.CloseHandle(handle);
                }
            }
        }

    }
}