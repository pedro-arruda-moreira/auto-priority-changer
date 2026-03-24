using System;
using System.Collections.Generic;

namespace AutoPriorityChanger.Config
{
    public class ConfigDTO
    {
        public Dictionary<String, String[]> processgroups;
        public Dictionary<String, ProcessDTO> processes;
        public List<String> exclusions;
        public Int32 timeout;
        public Int32 debug;

    }
}