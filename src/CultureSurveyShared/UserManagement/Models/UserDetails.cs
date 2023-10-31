using System.Collections.Generic;
using System.Linq;

namespace CultureSurveyShared.UserManagement.Models
{
    public class UserDetails
    {
        public string ID { get; set; }
        public string Email { get; set; }
        public string Username { get; set; }
        public IEnumerable<Claim> Claims { get; set; }

        public bool IsManager()
        {
            return Claims != null && Claims.Any(claim => claim.Type == ClaimType.Role &&
                                       claim.Value == GptwIdentityRoles.Manager.GetValue);
        }
        public bool IsAdmin()
        {
            return Claims != null && Claims.Any(claim => claim.Type == ClaimType.Role &&
                                       claim.Value == GptwIdentityRoles.HRAdmin.GetValue);
        }
        public bool IsUKGUser => Claims != null && Claims.Any(claim => claim.Type == ClaimType.TenantId);

        public string GetEmprisingRole()
        {
            return Claims?
                .SingleOrDefault(claim => claim.Type == ClaimType.Role && _allowedRoles.Contains(claim.Value))?.Value;
        }

        public string GetClient()
        {
            return Claims.FirstOrDefault(claim => claim.Type == ClaimType.ClientId)?.Value;
        }
        public string GetPreferredLanguage()
        {
            return Claims.FirstOrDefault(claim => claim.Type == ClaimType.Language)?.Value;
        }
        public string GetEmailFromClaim()
        {
            return Claims.FirstOrDefault(claim => claim.Type == ClaimType.Email)?.Value ?? Claims.FirstOrDefault(claim => claim.Type == ClaimType.Upn)?.Value;
        }
        public bool CredentialsDiffer(string firstName, string lastName)
        {
            return !Claims.Any(claim => claim.Type == ClaimType.GivenName && claim.Value == firstName)
                   || !Claims.Any(claim => claim.Type == ClaimType.FamilyName && claim.Value == lastName);
        }

        private List<string> _allowedRoles => new List<string> { GptwIdentityRoles.Manager.SetValue, GptwIdentityRoles.HRAdmin.SetValue };
    }
}
