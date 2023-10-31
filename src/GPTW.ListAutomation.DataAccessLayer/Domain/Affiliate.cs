using System;
using System.Collections.Generic;

namespace GPTW.ListAutomation.DataAccessLayer.Domain
{
    public partial class Affiliate
    {
        public Affiliate()
        {
            Countries = new HashSet<Country>();
            ListRequests = new HashSet<ListRequest>();
        }

        public string AffiliateName { get; set; }
        public string AffiliateId { get; set; }

        public virtual ICollection<Country> Countries { get; set; }
        public virtual ICollection<ListRequest> ListRequests { get; set; }
    }
}
