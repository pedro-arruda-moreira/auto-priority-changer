using System;
using System.IO;
using System.Collections.Generic;
using System.Web.Script.Serialization;
using System.Diagnostics;

namespace AutoPriorityChanger.Config
{
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