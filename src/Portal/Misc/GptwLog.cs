
//namespace Portal.Misc
//{
//    public class GptwLog
//    {
//    }
//}


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
using Newtonsoft.Json;
using MongoDB.Bson;
using MongoDB.Driver;
using Microsoft.ApplicationInsights;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.InteropExtensions;

/// <summary>

/// ''' Class to Log data

/// ''' </summary>

/// ''' <remarks></remarks>
public class GptwLog
{
    public GptwLogConfigInfo _GptwLogConfigInfo { get; set; }

    public GptwLog(GptwLogConfigInfo lci)
    {
        _GptwLogConfigInfo = lci;
    }

    public void LogInformation(string EntryDetail, GptwLogContext context = null)
    {
        Log("Information", EntryDetail, context);
    }

    public void LogWarning(string EntryDetail, GptwLogContext context = null)
    {
        Log("Warning", EntryDetail, context);
    }

    // TODO - Eventually want to completely remove this method in favor of LogErrorStringOnly and LogErrorWithException
    public void LogError(string EntryDetail, GptwLogContext context = null)
    {
        LogErrorStringOnly(EntryDetail, context);
    }

    public void LogErrorStringOnly(string EntryDetail, GptwLogContext context = null)
    {
        Log("Error", EntryDetail, context); 
        if (_GptwLogConfigInfo.AppInsightsTelemetryClient != null)
            _GptwLogConfigInfo.AppInsightsTelemetryClient.TrackEvent(String.Format("Error:{0}", EntryDetail));
        System.Threading.Thread.Sleep(25);
    }

    public void LogErrorWithException(string EntryDetail, Exception ex, GptwLogContext context = null)
    {
        Log("Error", EntryDetail + ", ex:" + GetDetailedExceptionString(ex), context);
        if (ex != null && _GptwLogConfigInfo.AppInsightsTelemetryClient != null)
            _GptwLogConfigInfo.AppInsightsTelemetryClient.TrackException(ex);
        System.Threading.Thread.Sleep(25);
    }

    public string GetDetailedExceptionString(Exception ex)
    {
        string ExceptionString = "";
        Exception WorkingEx = ex;
        while (WorkingEx != null)
        {
            ExceptionString += WorkingEx.Message + " | " + WorkingEx.StackTrace;
            WorkingEx = WorkingEx.InnerException;
        }
        return ExceptionString;
    }

    private void Log(string EntryType, string EntryDetail, GptwLogContext context)
    {
        if (EntryType == "Information")
            System.Diagnostics.Trace.TraceInformation(EntryDetail);
        else if (EntryType == "Warning")
            System.Diagnostics.Trace.TraceWarning(EntryDetail);
        else if (EntryType == "Error")
            System.Diagnostics.Trace.TraceError(EntryDetail);
        // create log item
        var pst = new GptwLogDetail() { EntryType = EntryType, Application = _GptwLogConfigInfo.ApplicationName, Environment = _GptwLogConfigInfo.ApplicationEnvironment, EntryDetail = EntryDetail };
        
        if (context != null)
        {
            if (context.ClientId != -1)
                pst.ClientId = context.ClientId.ToString();
            if (context.EngagementId != -1)
                pst.EngagementId = context.EngagementId.ToString();
            pst.EntryDetail = context.MethodName + " " + EntryDetail;
            pst.SessionId = context.SessionId;
            pst.Email = context.Email;
        }

        // SAVE TO MONGO
        try
        {
            MongoClient mongoClient = new MongoClient(_GptwLogConfigInfo.MongoConnectionString);
            IMongoDatabase mongoTestDb = mongoClient.GetDatabase("atlaslogs");
            IMongoCollection<GptwLogDetail> mongoSurveyResponsesCollection = mongoTestDb.GetCollection<GptwLogDetail>("logs_" + DateTime.Now.ToString("yyyyMM"));

            mongoSurveyResponsesCollection.InsertOne(pst);
        }
        catch (Exception ex)
        {
        }

        ////PUSH TO SERVICE BUS FOR REAL TIME MONITORING
        //try
        //{
        //    SendMessage(_GptwLogConfigInfo.ServiceBusConnectionString, pst).RunSynchronously();
        //}
        //catch (Exception ex)
        //{
        //}
    }

    static async Task SendMessage(string connectionString, GptwLogDetail pst)
    {
        IQueueClient queueClient = new QueueClient(connectionString, "rtlog");
        // Create a new message to send to the queue
        var logMessage = new Message();

        logMessage.UserProperties.Add("Environment", pst.Environment);
        logMessage.UserProperties.Add("Application", pst.Application);
        logMessage.UserProperties.Add("EntryType", pst.EntryType);
        logMessage.UserProperties.Add("EntryDetail", pst.EntryDetail);
        logMessage.UserProperties.Add("ClientId", pst.ClientId);
        logMessage.UserProperties.Add("EngagementId", pst.EngagementId);
        logMessage.UserProperties.Add("Email", pst.Email);
        logMessage.UserProperties.Add("SessionId", pst.SessionId);
        logMessage.TimeToLive = new TimeSpan(1, 0, 0);

        await queueClient.SendAsync(logMessage);
    }

}

public class GptwLogContext
{
    public int ClientId { get; set; } = -1;
    public int EngagementId { get; set; } = -1;
    // These are automatically set when you call the controller base class method GetNewGptwLogContext
    public string Email { get; set; } = "";
    public string SessionId { get; set; } = "";
    public string MethodName { get; set; } = "";
}

public class GptwLogDetail
{
    public string EntryType { get; set; }
    public string Application { get; set; }
    public string Environment { get; set; }
    public string EntryDetail { get; set; }
    public DateTime EntryDateTime { get; set; } = DateTime.UtcNow;
    public string ClientId { get; set; } = "";
    public string EngagementId { get; set; } = "";
    public string Email { get; set; } = "";
    public string SessionId { get; set; } = "";
}

public class GptwLogConfigInfo
{
    public string ApplicationName { get; set; }
    public string ApplicationEnvironment { get; set; }
    public string MongoConnectionString { get; set; }
    public string ServiceBusConnectionString { get; set; }
    public TelemetryClient AppInsightsTelemetryClient { get; set; }

    // FOR NEW APP INSIGHTS LOGGING (with telemetry client)
    public GptwLogConfigInfo(string AppName, string AppEnv, string MongoConnStr, string ServBusConnStr, TelemetryClient TelemClient)
    {
        ApplicationName = AppName;
        ApplicationEnvironment = AppEnv;
        MongoConnectionString = MongoConnStr;
        ServiceBusConnectionString = ServBusConnStr;
        AppInsightsTelemetryClient = TelemClient;
    }

    // TODO - Eventually want to completely remove this method in favor of LogErrorStringOnly and LogErrorWithException
    // OLD WAY OF LOGGING (no telemetry client)
    //public GptwLogConfigInfo(string AppName, string AppEnv, string MongoConnStr, string ServBusConnStr)
    //{
    //    ApplicationName = AppName;
    //    ApplicationEnvironment = AppEnv;
    //    MongoConnectionString = MongoConnStr;
    //    ServiceBusConnectionString = ServBusConnStr;
    //}
}
