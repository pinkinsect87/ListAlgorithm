using System;
using System.Collections.Generic;

namespace GPTW.ListAutomation.DataAccessLayer.Domain
{
    public partial class Country
    {
        public string CountryCode { get; set; }
        public string CountryName { get; set; }
        public string AffiliateId { get; set; }

        public virtual Affiliate Affiliate { get; set; }
    }
}
