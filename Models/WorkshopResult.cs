﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpeedMann.UpdateChecker.Models
{
    public enum WorkshopResult
    {
        None,
        NoRestrictions,
        NotWhitelisted,
        Blacklisted,
        Allowed,
        Banned,
        PrivateVisibility,
        Unknown,
    }
}
