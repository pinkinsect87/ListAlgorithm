using CultureSurveyShared.UserManagement.Helpers;
using CultureSurveyShared.UserManagement.Models;
using System;
using System.Threading.Tasks;

namespace CultureSurveyShared.UserManagement
{
    public interface IUserManager : IDisposable
    {
        Task UpdateUser(User user);
        Task<UserDetails> GetUserById(string userId);
        Task<UserDetails> GetUserByEmail(string email);
        //Task<List<UserInfo>> GetUsersByUserIds(List<string> ids);
        //Task<List<LastActiveUser>> GetLastActiveUsersByClients(List<string> clientIds);
        Task DeleteUser(UserDetails user);
        //Task<List<UserInfo>> SearchClientUsers(string clientId, string searchCriteria, string gptwIdentityRole);
        //Task<List<UserDetails>> GetGptwUsers(string affiliateId);
        HttpSender HttpSender { get; }
    }
}
