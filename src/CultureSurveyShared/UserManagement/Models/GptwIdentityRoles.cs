using System.Collections.Generic;

namespace CultureSurveyShared.UserManagement.Models
{
    public static class GptwIdentityRoles
    {
        public static class Manager
        {
            public static string SetValue { get { return "ClientAppRole_CMPHRManager"; } }

            public static string GetValue { get { return "ClientAppRole_CMPHRManager"; } }
        }

        public static class HRAdmin
        {
            public static string SetValue { get { return "ClientAppRole_CMPHRAdmin"; } }

            public static string GetValue { get { return "ClientAppRole_CMPHRAdmin"; } }
        }

        public static List<string> Roles = new List<string>
        {
            Manager.GetValue,
            HRAdmin.GetValue
        };

    }
}
