namespace CultureSurveyShared.UserManagement.Models
{
    public class Claim
    {
        public Claim(string type, string value)
        {
            Type = type;
            Value = value;
        }
        public Claim()
        {

        }
        public string Type { get; set; }
        public string Value { get; set; }
    }
}
