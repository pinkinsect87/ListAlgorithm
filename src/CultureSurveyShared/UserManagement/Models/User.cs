namespace CultureSurveyShared.UserManagement.Models
{
    public class User
    {
        public string ID { get; set; }
        public string Email { get; set; }
        public string Username { get; set; }
        public string Name { get; set; }
        public string GivenName { get; set; }
        public string FamilyName { get; set; }
        public string Role { get; set; }
        public string Upn { get; set; }
        public string ClientName { get; set; }
        public string ClientId { get; set; }
        public bool IsDefault { get; set; }
        public string Language { get; set; }
    }
}
