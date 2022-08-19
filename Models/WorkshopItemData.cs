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
        public string displayText;
        public bool required;
        public WorkshopResult result;
        public WorkshopItemData(ulong workshopId, string itemDisplayText, bool required)
        {
            this.id = workshopId;
            this.displayText = itemDisplayText;
            this.required = required;
            result = WorkshopResult.None;
        }
    }
}
