using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOTMotorDrivers.Helpers
{
    public struct ULN2003Progress
    {
        public bool IsActive { get; set; }
        public int CurrentStep { get; set; }
        public int StepsSize { get; set; }
        public int CurrentSequence { get; set; }
    }

}
