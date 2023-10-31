using CultureSurveyShared.UserManagement.Exceptions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using CultureSurveyShared.UserManagement.Helpers;
using CultureSurveyShared.UserManagement.Models;

namespace CultureSurveyShared.UserManagement
{
    public class UserManager : IUserManager
    {
        private string _urlBase;

        public HttpSender HttpSender { get; }

        public void Dispose()
        {
        }

        public UserManager(string urlBase, string username, string password)
        {
            _urlBase = urlBase;

            HttpSender = HttpSender.Generate(_urlBase, username, password);
            HttpSender.SetBaseUrl();
            HttpSender.SetAuthorization();
            HttpSender.SetAccept();
        }

        public async Task<UserDetails> GetUserById(string userId)
        {
            Guid guid = Guid.NewGuid();
            Debug.WriteLine($"Debug\t{DateTime.UtcNow:dd.MM HH:mm:ss}\t[{guid:N}] {MethodBase.GetCurrentMethod().ReflectedType.Name}.{MethodBase.GetCurrentMethod().Name} has started.");
            Debug.WriteLine($"Debug\t{DateTime.UtcNow:dd.MM HH:mm:ss}\t[{guid:N}] Parameters -> userId: {userId}");

            try
            {
                var requestedUrl = $"{_urlBase}Users?Id={userId}";

                var response = await HttpSender.GetAsync(requestedUrl);

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception(await response.Content.ReadAsStringAsync());
                }

                var responseBody = await response.Content.ReadAsStringAsync();

                var createdUser = (UserDetails)JsonConvert.DeserializeObject(responseBody, typeof(UserDetails));

                return createdUser;
            }
            catch (Exception e)
            {
                Trace.TraceError($"[{guid:N}] {MethodBase.GetCurrentMethod().ReflectedType.Name}.{MethodBase.GetCurrentMethod().Name}\t{e.Message}\n{e.StackTrace}\nEx:{JsonConvert.SerializeObject(e)}");
                throw;
            }
            finally
            {
                Debug.WriteLine(
                    $"Debug\t{DateTime.UtcNow:dd.MM HH:mm:ss}\t[{guid:N}] {MethodBase.GetCurrentMethod().ReflectedType.Name}.{MethodBase.GetCurrentMethod().Name} has finished.");
            }
        }

        public async Task<UserDetails> GetUserByEmail(string email)
        {
            Guid guid = Guid.NewGuid();
            Debug.WriteLine($"Debug\t{DateTime.UtcNow:dd.MM HH:mm:ss}\t[{guid:N}] {MethodBase.GetCurrentMethod().ReflectedType.Name}.{MethodBase.GetCurrentMethod().Name} has started.");
            Debug.WriteLine($"Debug\t{DateTime.UtcNow:dd.MM HH:mm:ss}\t[{guid:N}] Parameters -> email: {email}");

            try
            {
                var requestedUrl = $"{_urlBase}Users?email={HttpUtility.UrlEncode(email)}";

                var response = await HttpSender.GetAsync(requestedUrl);

                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    return null;
                }

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception(await response.Content.ReadAsStringAsync());
                }

                var responseBody = await response.Content.ReadAsStringAsync();

                var createdUser = (UserDetails)JsonConvert.DeserializeObject(responseBody, typeof(UserDetails));

                return createdUser;
            }
            catch (Exception e)
            {
                Trace.TraceError($"[{guid:N}] {MethodBase.GetCurrentMethod().ReflectedType.Name}.{MethodBase.GetCurrentMethod().Name}\t{e.Message}\n{e.StackTrace}\nEx:{JsonConvert.SerializeObject(e)}");
                throw;
            }
            finally
            {
                Debug.WriteLine(
                    $"Debug\t{DateTime.UtcNow:dd.MM HH:mm:ss}\t[{guid:N}] {MethodBase.GetCurrentMethod().ReflectedType.Name}.{MethodBase.GetCurrentMethod().Name} has finished.");
            }
        }

        public async Task UpdateUser(User user)
        {
            Guid guid = Guid.NewGuid();
            Debug.WriteLine($"Debug\t{DateTime.UtcNow:dd.MM HH:mm:ss}\t[{guid:N}] {MethodBase.GetCurrentMethod().ReflectedType.Name}.{MethodBase.GetCurrentMethod().Name} has started.");
            Debug.WriteLine($"Debug\t{DateTime.UtcNow:dd.MM HH:mm:ss}\t[{guid:N}] Parameters -> User: {JsonConvert.SerializeObject(user)}");

            try
            {
                var userid = new Guid(user.ID).ToString();

                IEnumerable<Claim> currentClaims = (await GetUserById(userid)).Claims;

                List<Claim> claims = SetClaims(user);

                var (roleClaimToAdd, roleClaimToRemove) = GetRoleClaims(user, currentClaims);

                var updateUserRequest = UpdateUserRequest.GenerateUpdateUserRequest(user, claims, currentClaims, roleClaimToAdd, roleClaimToRemove);

                var jsonRequest = JsonConvert.SerializeObject(updateUserRequest);
                var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

                var response = await HttpSender.PutAsync($"{_urlBase}Users/{userid}", content);

                if (!response.IsSuccessStatusCode)
                {
                    throw new IdentityException(await response.Content.ReadAsStringAsync());
                }
            }
            catch (IdentityException) { throw; }
            catch (Exception e)
            {
                Trace.TraceError($"[{guid:N}] {MethodBase.GetCurrentMethod().ReflectedType.Name}.{MethodBase.GetCurrentMethod().Name}\t{e.Message}\n{e.StackTrace}\nEx:{JsonConvert.SerializeObject(e)}");
                throw;
            }
            finally
            {
                Debug.WriteLine(
                    $"Debug\t{DateTime.UtcNow:dd.MM HH:mm:ss}\t[{guid:N}] {MethodBase.GetCurrentMethod().ReflectedType.Name}.{MethodBase.GetCurrentMethod().Name} has finished.");
            }
        }

        //public async Task<List<UserInfo>> SearchClientUsers(string clientId, string searchCriteria, string gptwIdentityRole)
        //{
        //    Guid guid = Guid.NewGuid();
        //    Debug.WriteLine($"Debug\t{DateTime.UtcNow:dd.MM HH:mm:ss}\t[{guid:N}] {MethodBase.GetCurrentMethod().ReflectedType.Name}.{MethodBase.GetCurrentMethod().Name} has started.");
        //    Debug.WriteLine($"Debug\t{DateTime.UtcNow:dd.MM HH:mm:ss}\t[{guid:N}] Parameters -> clientId: {clientId}, searchCriteria: {searchCriteria}");

        //    try
        //    {
        //        var uriPath = new StringBuilder();

        //        uriPath.Append($"api/Users/{clientId}");

        //        if (!string.IsNullOrWhiteSpace(searchCriteria) || !string.IsNullOrWhiteSpace(gptwIdentityRole))
        //        {
        //            uriPath.Append("?");

        //            var queryString = HttpUtility.ParseQueryString(string.Empty);

        //            if (!string.IsNullOrWhiteSpace(searchCriteria))
        //            {
        //                queryString[nameof(searchCriteria)] = searchCriteria;
        //            }

        //            if (!string.IsNullOrWhiteSpace(gptwIdentityRole))
        //            {
        //                queryString[nameof(gptwIdentityRole)] = gptwIdentityRole;
        //            }

        //            uriPath.Append(queryString);
        //        }

        //        var response = await HttpSender.GetAsync(uriPath.ToString());

        //        if (!response.IsSuccessStatusCode)
        //        {
        //            throw new Exception(await response.Content.ReadAsStringAsync());
        //        }
        //        var responseBody = await response.Content.ReadAsStringAsync();

        //        var users = (List<UserInfo>)JsonConvert.DeserializeObject(responseBody, typeof(List<UserInfo>));
        //        return users;
        //    }
        //    catch (Exception e)
        //    {
        //        Trace.TraceError($"[{guid:N}] {MethodBase.GetCurrentMethod().ReflectedType.Name}.{MethodBase.GetCurrentMethod().Name}\t{e.Message}\n{e.StackTrace}\nEx:{JsonConvert.SerializeObject(e)}");
        //        throw;
        //    }
        //    finally
        //    {
        //        Debug.WriteLine(
        //            $"Debug\t{DateTime.UtcNow:dd.MM HH:mm:ss}\t[{guid:N}] {MethodBase.GetCurrentMethod().ReflectedType.Name}.{MethodBase.GetCurrentMethod().Name} has finished.");
        //    }
        //}

        //public async Task<List<UserInfo>> GetUsersByUserIds(List<string> userIds)
        //{
        //    Guid guid = Guid.NewGuid();
        //    Debug.WriteLine($"Debug\t{DateTime.UtcNow:dd.MM HH:mm:ss}\t[{guid:N}] {MethodBase.GetCurrentMethod().ReflectedType.Name}.{MethodBase.GetCurrentMethod().Name} has started.");
        //    Debug.WriteLine($"Debug\t{DateTime.UtcNow:dd.MM HH:mm:ss}\t[{guid:N}] Parameters -> userIds: {JsonConvert.SerializeObject(userIds)}");

        //    try
        //    {
        //        var request = new UserIds
        //        {
        //            Ids = userIds
        //        };
        //        var jsonRequest = JsonConvert.SerializeObject(request);
        //        var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

        //        var response = await HttpSender.PostAsync("api/Users/All", content);

        //        if (response.StatusCode == HttpStatusCode.NotFound)
        //        {
        //            return new List<UserInfo>();
        //        }

        //        if (!response.IsSuccessStatusCode)
        //        {
        //            throw new Exception(await response.Content.ReadAsStringAsync());
        //        }

        //        var responseBody = await response.Content.ReadAsStringAsync();

        //        var users = JsonConvert.DeserializeObject<List<UserInfo>>(responseBody);

        //        return users;
        //    }
        //    catch (Exception e)
        //    {
        //        Trace.TraceError($"[{guid:N}] {MethodBase.GetCurrentMethod().ReflectedType.Name}.{MethodBase.GetCurrentMethod().Name}\t{e.Message}\n{e.StackTrace}\nEx:{JsonConvert.SerializeObject(e)}");
        //        throw;
        //    }
        //    finally
        //    {
        //        Debug.WriteLine(
        //            $"Debug\t{DateTime.UtcNow:dd.MM HH:mm:ss}\t[{guid:N}] {MethodBase.GetCurrentMethod().ReflectedType.Name}.{MethodBase.GetCurrentMethod().Name} has finished.");
        //    }
        //}

        //public async Task<List<LastActiveUser>> GetLastActiveUsersByClients(List<string> clientIds)
        //{
        //    Guid guid = Guid.NewGuid();
        //    Debug.WriteLine($"Debug\t{DateTime.UtcNow:dd.MM HH:mm:ss}\t[{guid:N}] {MethodBase.GetCurrentMethod().ReflectedType.Name}.{MethodBase.GetCurrentMethod().Name} has started.");
        //    Debug.WriteLine($"Debug\t{DateTime.UtcNow:dd.MM HH:mm:ss}\t[{guid:N}] Parameters -> ids: {JsonConvert.SerializeObject(clientIds)}");

        //    try
        //    {
        //        var jsonRequest = JsonConvert.SerializeObject(clientIds);
        //        var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

        //        var response = await HttpSender.PostAsync("api/Users/LastActiveUsers", content);

        //        if (response.StatusCode == HttpStatusCode.NotFound)
        //        {
        //            return new List<LastActiveUser>();
        //        }

        //        if (!response.IsSuccessStatusCode)
        //        {
        //            throw new Exception(await response.Content.ReadAsStringAsync());
        //        }

        //        var responseBody = await response.Content.ReadAsStringAsync();

        //        var users = (List<LastActiveUser>)JsonConvert.DeserializeObject(responseBody, typeof(List<LastActiveUser>));

        //        return users;
        //    }
        //    catch (Exception e)
        //    {
        //        Trace.TraceError($"[{guid:N}] {MethodBase.GetCurrentMethod().ReflectedType.Name}.{MethodBase.GetCurrentMethod().Name}\t{e.Message}\n{e.StackTrace}\nEx:{JsonConvert.SerializeObject(e)}");
        //        throw;
        //    }
        //    finally
        //    {
        //        Debug.WriteLine(
        //            $"Debug\t{DateTime.UtcNow:dd.MM HH:mm:ss}\t[{guid:N}] {MethodBase.GetCurrentMethod().ReflectedType.Name}.{MethodBase.GetCurrentMethod().Name} has finished.");
        //    }
        //}

        public async Task DeleteUser(UserDetails user)
        {
            Guid guid = Guid.NewGuid();
            Debug.WriteLine($"Debug\t{DateTime.UtcNow:dd.MM HH:mm:ss}\t[{guid:N}] {MethodBase.GetCurrentMethod().ReflectedType.Name}.{MethodBase.GetCurrentMethod().Name} has started.");
            Debug.WriteLine($"Debug\t{DateTime.UtcNow:dd.MM HH:mm:ss}\t[{guid:N}] Parameters -> user[{user}]");

            try
            {
                var userid = new Guid(user.ID).ToString();

                var claims = (await GetUserById(userid)).Claims;

                var currentRoleClaim = GetCurrentRoleClaim(claims);
                if (currentRoleClaim == null)
                {
                    return;
                }

                var updateUserRequest = UpdateUserRequest.GenerateDeleteUserRequest(user.Username, user.Email, currentRoleClaim);

                var jsonRequest = JsonConvert.SerializeObject(updateUserRequest);
                var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

                var response = await HttpSender.PutAsync($"{_urlBase}Users/{userid}", content);

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception(await response.Content.ReadAsStringAsync());
                }
            }
            catch (Exception e)
            {
                Trace.TraceError($"[{guid:N}] {MethodBase.GetCurrentMethod().ReflectedType.Name}.{MethodBase.GetCurrentMethod().Name}\t{e.Message}\n{e.StackTrace}");
                throw;
            }
            finally
            {
                Debug.WriteLine($"Debug\t{DateTime.UtcNow:dd.MM HH:mm:ss}\t[{guid:N}] {MethodBase.GetCurrentMethod().ReflectedType.Name}.{MethodBase.GetCurrentMethod().Name} has finished.");
            }
        }

        //public async Task<List<UserDetails>> GetGptwUsers(string affiliateId)
        //{
        //    Guid guid = Guid.NewGuid();
        //    Debug.WriteLine($"Debug\t{DateTime.UtcNow:dd.MM HH:mm:ss}\t[{guid:N}] {MethodBase.GetCurrentMethod().ReflectedType.Name}.{MethodBase.GetCurrentMethod().Name} has started.");
        //    Debug.WriteLine($"Debug\t{DateTime.UtcNow:dd.MM HH:mm:ss}\t[{guid:N}] Parameters -> affiliateId: {affiliateId}");

        //    try
        //    {
        //        var claim = IdentityHelper.GenerateAffiliateClaim(affiliateId);

        //        return await GetUsersByClaim(new Claim(claim.type, claim.value));
        //    }
        //    catch (Exception e)
        //    {
        //        Trace.TraceError($"[{guid:N}] {MethodBase.GetCurrentMethod().ReflectedType.Name}.{MethodBase.GetCurrentMethod().Name}\t{e.Message}\n{e.StackTrace}\nEx:{JsonConvert.SerializeObject(e)}");
        //        throw;
        //    }
        //    finally
        //    {
        //        Debug.WriteLine(
        //            $"Debug\t{DateTime.UtcNow:dd.MM HH:mm:ss}\t[{guid:N}] {MethodBase.GetCurrentMethod().ReflectedType.Name}.{MethodBase.GetCurrentMethod().Name} has finished.");
        //    }
        //}

        //private async Task<List<UserDetails>> GetUsersByClaim(Claim claim, List<GptwOptionalRoles> roles = null)
        //{
        //    Guid guid = Guid.NewGuid();
        //    Debug.WriteLine($"Debug\t{DateTime.UtcNow:dd.MM HH:mm:ss}\t[{guid:N}] {MethodBase.GetCurrentMethod().ReflectedType.Name}.{MethodBase.GetCurrentMethod().Name} has started.");
        //    Debug.WriteLine($"Debug\t{DateTime.UtcNow:dd.MM HH:mm:ss}\t[{guid:N}] Parameters -> claim: {JsonConvert.SerializeObject(claim)}, roles: {JsonConvert.SerializeObject(roles)}");

        //    try
        //    {
        //        var jsonRequest = JsonConvert.SerializeObject(new { claim.Type, claim.Value, Roles = roles },
        //            new JsonSerializerSettings()
        //            {
        //                NullValueHandling = NullValueHandling.Ignore
        //            });
        //        var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

        //        var response = await HttpSender.PostAsync("api/Users/Clients", content);

        //        if (!response.IsSuccessStatusCode)
        //        {
        //            throw new Exception(await response.Content.ReadAsStringAsync());
        //        }

        //        var responseBody = await response.Content.ReadAsStringAsync();

        //        var users = (List<UserDetails>)JsonConvert.DeserializeObject(responseBody, typeof(List<UserDetails>));

        //        return users;
        //    }
        //    catch (Exception e)
        //    {
        //        Trace.TraceError($"[{guid:N}] {MethodBase.GetCurrentMethod().ReflectedType.Name}.{MethodBase.GetCurrentMethod().Name}\t{e.Message}\n{e.StackTrace}\nEx:{JsonConvert.SerializeObject(e)}");
        //        throw;
        //    }
        //    finally
        //    {
        //        Debug.WriteLine(
        //            $"Debug\t{DateTime.UtcNow:dd.MM HH:mm:ss}\t[{guid:N}] {MethodBase.GetCurrentMethod().ReflectedType.Name}.{MethodBase.GetCurrentMethod().Name} has finished.");
        //    }
        //}

        private List<Claim> SetClaims(User user)
        {
            Guid guid = Guid.NewGuid();
            Debug.WriteLine($"Debug\t{DateTime.UtcNow:dd.MM HH:mm:ss}\t[{guid:N}] {MethodBase.GetCurrentMethod().ReflectedType.Name}.{MethodBase.GetCurrentMethod().Name} has started.");
            Debug.WriteLine($"Debug\t{DateTime.UtcNow:dd.MM HH:mm:ss}\t[{guid:N}] Parameters -> User: {JsonConvert.SerializeObject(user)}");

            try
            {
                var claims = new List<Claim>();

                if (!string.IsNullOrEmpty(user.ClientId))
                {
                    claims.Add(new Claim
                    {
                        Type = ClaimType.ClientId,
                        Value = user.ClientId
                    });
                }

                if (user.IsDefault)
                {
                    claims.Add(new Claim
                    {
                        Type = ClaimType.IsDefault,
                        Value = "true"
                    });
                }

                if (!string.IsNullOrEmpty(user.ClientName))
                {
                    claims.Add(new Claim
                    {
                        Type = ClaimType.ClientName,
                        Value = user.ClientName
                    });
                }

                if (!string.IsNullOrEmpty(user.Name))
                {
                    claims.Add(new Claim
                    {
                        Type = ClaimType.Name,
                        Value = user.Name
                    });
                }

                if (!string.IsNullOrEmpty(user.GivenName))
                {
                    claims.Add(new Claim
                    {
                        Type = ClaimType.GivenName,
                        Value = user.GivenName
                    });
                }

                if (!string.IsNullOrEmpty(user.FamilyName))
                {
                    claims.Add(new Claim
                    {
                        Type = ClaimType.FamilyName,
                        Value = user.FamilyName
                    });
                }

                if (!string.IsNullOrEmpty(user.Upn))
                {
                    claims.Add(new Claim
                    {
                        Type = ClaimType.Upn,
                        Value = user.Upn
                    });
                }

                if (!string.IsNullOrEmpty(user.Language))
                {
                    claims.Add(new Claim
                    {
                        Type = ClaimType.Language,
                        Value = user.Language
                    });
                }

                return claims;
            }
            catch (Exception e)
            {
                Trace.TraceError($"[{guid:N}] {MethodBase.GetCurrentMethod().ReflectedType.Name}.{MethodBase.GetCurrentMethod().Name}\t{e.Message}\n{e.StackTrace}\nEx:{JsonConvert.SerializeObject(e)}");
                throw;
            }
            finally
            {
                Debug.WriteLine(
                    $"Debug\t{DateTime.UtcNow:dd.MM HH:mm:ss}\t[{guid:N}] {MethodBase.GetCurrentMethod().ReflectedType.Name}.{MethodBase.GetCurrentMethod().Name} has finished.");
            }
        }

        private (Claim add, Claim remove) GetRoleClaims(User user, IEnumerable<Claim> currentClaims)
        {
            string role = user.Role;
            Claim currentRoleClaim = GetCurrentRoleClaim(currentClaims);
            Claim add = null, remove = null;
            if (currentRoleClaim != null && string.IsNullOrWhiteSpace(role))
            {
                remove = currentRoleClaim;
            }
            else if (currentRoleClaim?.Value != role)
            {
                if (role == GptwIdentityRoles.HRAdmin.GetValue)
                {
                    add = new Claim { Type = ClaimType.Role, Value = GptwIdentityRoles.HRAdmin.SetValue };
                }
                else
                {
                    add = new Claim { Type = ClaimType.Role, Value = GptwIdentityRoles.Manager.SetValue };
                }
                if (currentRoleClaim != null)
                {
                    remove = currentRoleClaim;
                }
            }
            return (add, remove);
        }

        private Claim GetCurrentRoleClaim(IEnumerable<Claim> currentClaims)
        {
            return currentClaims.FirstOrDefault(claim => claim.Type == ClaimType.Role &&
                                                         (claim.Value == GptwIdentityRoles.HRAdmin.GetValue || claim.Value == GptwIdentityRoles.Manager.GetValue));
        }

    }
}
