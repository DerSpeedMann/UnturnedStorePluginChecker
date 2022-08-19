using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpeedMann.UpdateChecker.Models.UStore
{
    public class WorkshopItem
    {
        public ulong workshopFileId;
        public bool required;
        public WorkshopResult result = WorkshopResult.None;
        public WorkshopItem()
        {

        }
        public WorkshopItem(ulong workshopId, bool required)
        {
            this.workshopFileId = workshopId;
            this.required = required;
            result = WorkshopResult.None;
        }
    }
}
