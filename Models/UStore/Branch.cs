using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpeedMann.UpdateChecker.Models.UStore
{
    public class Branch
    {
        public string name = "";
        public List<Version> versions = new List<Version>();
    }
}
