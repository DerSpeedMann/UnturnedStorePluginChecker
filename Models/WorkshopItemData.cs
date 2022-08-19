using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpeedMann.UpdateChecker.Models
{
    public class WorkshopItemData
    {
        public ulong id;
        public bool required;
        public WorkshopResult result;
        public WorkshopItemData(ulong workshopId, bool required)
        {
            this.id = workshopId;
            this.required = required;
            result = WorkshopResult.None;
        }
    }
}
