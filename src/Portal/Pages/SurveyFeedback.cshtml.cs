using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ZendeskApi_v2.Models.Tickets;
using System.Text;
using Microsoft.Extensions.Options;
using Portal.Model;

namespace Portal.Pages
{
    [BindProperties]
    public class SurveyFeedbackModel : PageModel
    {
        public string name { get; set; } = "";
        public string email { get; set; } = "";
        public string company { get; set; } = "";
        public string phone { get; set; } = "";
        public string comment { get; set; } = "";
        public bool submitted { get; set; } = false;
        public bool submissionError { get; set; } = false;
        public string currentYear { get; set; } = "";

        private AppSettings _appsettings;
        public SurveyFeedbackModel(IOptions<AppSettings> options)
        {
            _appsettings = options.Value;
        }

        public void OnGet()
        {
            currentYear = DateTime.Now.Year.ToString();
        }

        public void OnPost()
        {
            SubmitFeedback();
        }

        public void CloseConfirmationDialog()
        {
            //this doesn't work
            //this._winRef.nativeWindow.close();

            //this._winRef.nativeWindow.location.href = 'https://www.greatplacetowork.com';
        }

        public void SubmitFeedback()
        {
            submitted = true;
            CreateHelpDeskTicket(email, comment, company, name, phone);
        }

        public void CreateHelpDeskTicket(string email, string comment, string companyname, string name, string phone)
        {
            try
            {
                ZendeskApi_v2.ZendeskApi api = new ZendeskApi_v2.ZendeskApi(_appsettings.ZendeskAPIUrl, _appsettings.ZendeskUsername, _appsettings.ZendeskPassword);

                StringBuilder allinfo = new StringBuilder();
                allinfo.AppendFormat("Name: {0}", name).AppendLine();
                allinfo.AppendFormat("Phone: {0}", phone).AppendLine();
                allinfo.AppendFormat("Email: {0}", email).AppendLine();
                allinfo.AppendFormat("Company: {0}", companyname).AppendLine();
                allinfo.AppendFormat("Comment: {0}", comment).AppendLine();

                IndividualTicketResponse response = api.Tickets.CreateTicket
                (
                    new ZendeskApi_v2.Models.Tickets.Ticket
                    {
                        Status = "new",
                        RequesterId = _appsettings.ZendeskRequesterId,
                        GroupId = _appsettings.ZendeskGroupId,
                        Subject = "TI Integrity " + companyname,
                        Type = "Incident",
                        Comment = new ZendeskApi_v2.Models.Tickets.Comment
                        {
                            Body = allinfo.ToString(),
                            Public = false,
                        }
                    }
                 );

                if (response == null || response.Ticket == null)
                {
                    submissionError = true;
                }
                else
                {
                    submissionError = false;
                }
            }
            catch (Exception e)
            {
                // AtlasLog.LogErrorWithException(String.Format("Unhandled Exception caught."), e, gptwContext);
                submissionError = true;
            }
        }
    }
}
