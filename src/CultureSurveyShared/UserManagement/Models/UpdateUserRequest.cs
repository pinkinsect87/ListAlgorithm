using System.Collections.Generic;
using System.Linq;

namespace CultureSurveyShared.UserManagement.Models
{
    public class UpdateUserRequest
    {
        private UpdateUserRequest(string username, string email,
            IEnumerable<Claim> addClaims = null, IEnumerable<Claim> replaceClaims = null, IEnumerable<Claim> removeClaims = null)
        {
            Username = username;
            Email = email;

            if (addClaims != null)
            {
                AddClaims = addClaims;
            }
            if (replaceClaims != null)
            {
                ReplaceClaims = replaceClaims;
            }
            if (removeClaims != null)
            {
                RemoveClaims = removeClaims;
            }
        }
        public static UpdateUserRequest GenerateDeleteUserRequest(string username, string email, Claim removeClaim)
        {
            return new UpdateUserRequest(username, email, removeClaims: new List<Claim> { removeClaim });
        }
        public static UpdateUserRequest GenerateUpdateUserRequest(User user, IEnumerable<Claim> claims, IEnumerable<Claim> currentClaims, Claim roleClaimToAdd, Claim roleClaimToRemove)
        {
            var addClaims = claims.Except(currentClaims, claim => claim.Type).ToList();
            var replaceClaims = claims.Intersect(currentClaims, claim => claim.Type);
            var removeClaims = new List<Claim>();
            if (roleClaimToAdd != null)
            {
                addClaims.Add(roleClaimToAdd);
            }
            if (roleClaimToRemove != null)
            {
                removeClaims.Add(roleClaimToRemove);
            }

            return new UpdateUserRequest(user.Username, user.Email, addClaims, replaceClaims, removeClaims);
        }

        /// <summary>
        /// The user name of the user.
        /// </summary>
        public string Username { get; }

        /// <summary>
        /// The email address of the user.
        /// </summary>
        public string Email { get; }

        /// <summary>
        /// Adds the specified claims to the user.
        /// </summary>
        public IEnumerable<Claim> AddClaims { get; }
        /// <summary>
        /// Replaces the specified claims on the user with the new claims.
        /// </summary>
        public IEnumerable<Claim> ReplaceClaims { get; }
        /// <summary>
        /// Removes the specified claims from the user.
        /// </summary>
        public IEnumerable<Claim> RemoveClaims { get; }
    }
}
