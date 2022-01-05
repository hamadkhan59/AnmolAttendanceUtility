using System;

namespace BioMetrixCore
{
    public class MachineInfo
    {
        public int MachineNumber { get; set; }
        public int RegisterID { get; set; }
        public int LogID { get; set; }
        public string EnployeeName { get; set; }
        public string DateTimeRecord { get; set; }

        public DateTime DateOnlyRecord
        {
            get { return DateTime.Parse(DateTime.Parse(DateTimeRecord).ToString("yyyy-MM-dd")); }
        }
        public DateTime TimeOnlyRecord
        {
            get { return DateTime.Parse(DateTime.Parse(DateTimeRecord).ToString("hh:mm:ss tt")); }
        }

    }
}
