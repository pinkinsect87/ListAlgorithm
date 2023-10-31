
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;

namespace CultureSurveyShared
{
    public class CommonValidations
    {
        private static readonly Regex _emailValidationRegex = new Regex(@"^((([a-z]|\d|[!#\$%&'\*\+\-\/=\?\^_`{\|}~]|" + @"[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])" + @"+(\.([a-z]|\d|[!#\$%&'\*\+\-\/=\?\^_`{\|}~]|" + @"[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])+)*)|((\x22)" + @"((((\x20|\x09)*(\x0d\x0a))?(\x20|\x09)+)" + @"?(([\x01-\x08\x0b\x0c\x0e-\x1f\x7f]|\x21|[\x23-\x5b]" + @"|[\x5d-\x7e]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])" + @"|(\\([\x01-\x09\x0b\x0c\x0d-\x7f]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF]))))" + @"*(((\x20|\x09)*(\x0d\x0a))?(\x20|\x09)+)?(\x22)))" + "@" + @"((([a-z]|\d|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])" + @"|(([a-z]|\d|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])" + @"([a-z]|\d|-|\.|_|~|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])" + @"*([a-z]|\d|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])))\.)" + @"+(([a-z]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])|" + @"(([a-z]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])" + @"([a-z]|\d|-|\.|_|~|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])*([a-z]" + @"|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])))\.?$", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture | RegexOptions.Compiled);
        private static readonly Regex _notAllowedCharacters = new Regex("[^a-zA-Z0-9@~!$%^&*_=+}{'?.-]");

        //public bool EmailVerification(string email)
        //{

        //    try
        //    {
        //        MailAddress myEmail = new MailAddress(email);

        //        string validValues = "abcdefghijklmnopqrstuvwxyz0123456789@~!$^&*_=+{}'?-.%";
        //        bool valid = true;

        //        foreach (char c in email.ToLower())
        //        {
        //            int index = validValues.IndexOf(c);
        //            if (index < 0)
        //                valid = false;
        //        }
        //        return valid;
        //    }
        //    catch
        //    {
        //        return false;
        //    }

        //    return false;

        //}

        public static bool IsEmailAddressValid(string email)
        {
            var isValidFormat = _emailValidationRegex.Match(email).Length > 0;
            var invalidCharacters = GetInvalidCharacters(email);
            return isValidFormat && !invalidCharacters.Any();
        }

        private static HashSet<string> GetInvalidCharacters(string email)
        {
            var matches = _notAllowedCharacters.Matches(email);
            var invalidCharacters = new HashSet<string>();

            if (matches.Count > 0)
            {
                foreach (Match match in matches)
                    invalidCharacters.Add(match.Value);
            }

            return invalidCharacters;
        }

    }
}