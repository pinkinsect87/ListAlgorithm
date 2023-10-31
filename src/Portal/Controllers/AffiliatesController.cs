using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualBasic;
using System.Net;
//using System.Web.Http;
using SharedProject2;
using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Portal.Model;
using Portal.Controllers;
using System.Runtime.InteropServices;


namespace Affiliates.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AffiliatesController : PortalControllerBase
    {

        public AffiliatesController(TelemetryClient appInsights, IOptions<AppSettings> appSettings, IHttpClientFactory clientFactory) : base(appInsights, appSettings, clientFactory)
        {
        }

        [HttpGet("[action]")]
        public ReturnAffiliates GetAllAffiliates()
        {
            GptwLogContext gptwContext = GetNewGptwLogContext("GetAllAffiliates");

            ReturnAffiliates returnAffiliates = new ReturnAffiliates { IsSuccess = false, ErrorMessage = "", Affiliates = new List<Affiliate>() };

            try
            {
                if (!IsUserAuthorizedNotClientIdSpecific(gptwContext))
                {
                    AtlasLog.LogError(String.Format("Not Authorized.token:{0}", authorizationToken), gptwContext);
                    Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    return returnAffiliates;
                }

                List<Affiliate> myAffiliates = this.AORepository.GetAllAffiliates().ToList();
                foreach (var affiliate in myAffiliates)
                {
                    var affname = affiliate.AffiliateName;
                    affiliate.AffiliateName = affname + " (" + affiliate.AffiliateId + ")";
                }

                returnAffiliates.Affiliates = (from myaffiliate in myAffiliates orderby myaffiliate.AffiliateName select myaffiliate).ToList();
                returnAffiliates.IsSuccess = true;
            }
            catch (Exception e)
            {
                AtlasLog.LogErrorWithException(String.Format("Unhandled Exception caught."), e, gptwContext);
                returnAffiliates.ErrorMessage = e.Message;
                returnAffiliates.IsSuccess = false;
            }
            return returnAffiliates;
        }

        [HttpGet("[action]")]
        public ReturnCountries GetCountriesForAffiliate(string myAffiliateId)
        {
            GptwLogContext gptwContext = GetNewGptwLogContext("GetCountriesForAffiliate");
            ReturnCountries returnCountries = new ReturnCountries { IsSuccess = false, ErrorMessage = "", Countries = new List<Country>() };

            try
            {
                if (!IsUserAuthorizedNotClientIdSpecific(gptwContext))
                {
                    AtlasLog.LogError(String.Format("Not Authorized.token:{0}", authorizationToken), gptwContext);
                    Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    return returnCountries;
                }
                Affiliate myAffiliate = this.AORepository.GetAffiliatebyAffiliateId(myAffiliateId);

                returnCountries.Countries = (from myCountry in this.AORepository.GetAllCountries().ToList()
                                             where myAffiliate.AllowableCountryCodes.Contains(myCountry.CountryCode)
                                             orderby myCountry.CountryName
                                             select myCountry).ToList();
            }
            catch (Exception e)
            {
                AtlasLog.LogErrorWithException(String.Format("Unhandled Exception caught."), e, gptwContext);
                returnCountries.ErrorMessage = e.Message;
                returnCountries.IsSuccess = false;
            }
            return returnCountries;
        }

        [HttpGet("[action]")]
        public ReturnCountries GetAllCountries()
        {
            GptwLogContext gptwContext = GetNewGptwLogContext("GetAllCountries");

            ReturnCountries returnCountries = new ReturnCountries { IsSuccess = false, ErrorMessage = "", Countries = new List<Country>() };

            try
            {
                if (!IsUserAuthorizedNotClientIdSpecific(gptwContext))
                {
                    AtlasLog.LogError(String.Format("Not Authorized.token:{0}", authorizationToken), gptwContext);
                    Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    return returnCountries;
                }

                returnCountries.Countries = (from myCountry in this.AORepository.GetAllCountries().ToList() orderby myCountry.CountryName select myCountry).ToList();
                returnCountries.IsSuccess = true;
            }
            catch (Exception e)
            {
                AtlasLog.LogErrorWithException(String.Format("Unhandled Exception caught."), e, gptwContext);
                returnCountries.ErrorMessage = e.Message;
                returnCountries.IsSuccess = false;
            }
            return returnCountries;
        }

        [HttpGet("[action]")]
        public ReturnCountry GetCountryByCountryCode(string countryCode)
        {
            GptwLogContext gptwContext = GetNewGptwLogContext("GetCountryByCountryCode");

            ReturnCountry returnCountry = new ReturnCountry { IsSuccess = false, ErrorMessage = "", Country = new Country() };

            try
            {
                if (!IsUserAuthorizedNotClientIdSpecific(gptwContext))
                {
                    AtlasLog.LogError(String.Format("Not Authorized.token:{0}", authorizationToken), gptwContext);
                    Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    return returnCountry;
                }

                returnCountry.Country = this.AORepository.GetCountryByCountryCode(countryCode);
                returnCountry.IsSuccess = true;
            }
            catch (Exception e)
            {
                AtlasLog.LogErrorWithException(String.Format("Unhandled Exception caught."), e, gptwContext);
                returnCountry.ErrorMessage = e.Message;
                returnCountry.IsSuccess = false;
            }
            return returnCountry;
        }

        [HttpGet("[action]")]
        public ReturnLanguages GetLanguages()
        {
            GptwLogContext gptwContext = GetNewGptwLogContext("GetAllAffiliates");

            ReturnLanguages returnLanguages = new ReturnLanguages { IsSuccess = false, ErrorMessage = "", Languages = new List<Language>() };

            try
            {
                if (!IsUserAuthorizedNotClientIdSpecific(gptwContext))
                {
                    AtlasLog.LogError(String.Format("Not Authorized.token:{0}", authorizationToken), gptwContext);
                    Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    return returnLanguages;
                }

                returnLanguages.Languages = this.AORepository.GetAllLanguages();
            }
            catch (Exception e)
            {
                AtlasLog.LogErrorWithException(String.Format("Unhandled Exception caught."), e, gptwContext);
                returnLanguages.ErrorMessage = e.Message;
                returnLanguages.IsSuccess = false;
            }
            return returnLanguages;
        }
    }
}

public class ReturnAffiliates
{
    public bool IsSuccess { get; set; }
    public string ErrorMessage { get; set; }

    public List<Affiliate> Affiliates { get; set; }
}
public class ReturnCountries
{
    public bool IsSuccess { get; set; }
    public string ErrorMessage { get; set; }
    public List<Country> Countries { get; set; }
}
public class ReturnCountry
{
    public bool IsSuccess { get; set; }
    public string ErrorMessage { get; set; }
    public Country Country { get; set; }
}
public class ReturnLanguages
{
    public bool IsSuccess { get; set; }
    public string ErrorMessage { get; set; }
    public List<Language> Languages { get; set; }
}


