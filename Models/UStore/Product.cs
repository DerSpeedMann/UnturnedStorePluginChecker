using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpeedMann.PluginChecker.Models.UStore
{
    public class Product
    {
        public uint id;
        public List<Branch> branches = new List<Branch>();
        public List<WorkshopItem> workshopItems;
    }
}
