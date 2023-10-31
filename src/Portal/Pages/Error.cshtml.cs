using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Portal.Pages
{
    public class ErrorModel : PageModel
    {
        public void OnGet()
        {
            // Redirect to the angular general error page
            Response.Redirect("/error/general");
        }
    }
}
