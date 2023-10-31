using System;
using System.Collections.Generic;

namespace GPTW.ListAutomation.DataAccessLayer.Domain
{
    public partial class Status
    {
        public int StatusId { get; set; }
        public string StatusName { get; set; }
        public string? StatusDescription { get; set; }
    }
}
