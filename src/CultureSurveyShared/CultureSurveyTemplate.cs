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
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;


public class CultureSurveyTemplate
{
    public ObjectId Id { get; set; }
    public string SurveyType { get; set; } = ""; // TODO - could also be an enum
    public string TemplateVersion { get; set; } = "";
    public string SurveyTitle { get; set; } = "";
    public string SurveyIntroduction { get; set; } = "";
    public List<CultureSurveyQuestion> Questions { get; set; } = new List<CultureSurveyQuestion>();
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime CreatedDate { get; set; } = DateTime.Now;
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime LastModifiedDate { get; set; } = DateTime.Now;

    public CultureSurveyTemplate(string _surveyType, string _templateVersion)
    {
        SurveyType = _surveyType;
        TemplateVersion = _templateVersion;
    }

    public void SetLastModifiedDate()
    {
        LastModifiedDate = DateTime.UtcNow;
    }

    public List<CultureSurveyQuestion> FlattenQuestions()
    {
        List<CultureSurveyQuestion> flattendQuestions = new List<CultureSurveyQuestion>();
        foreach (CultureSurveyQuestion question in this.Questions)
        {
            FlattenQuestions(question, flattendQuestions);
        }
        return flattendQuestions;
    }

    public void FlattenQuestions(CultureSurveyQuestion question, List<CultureSurveyQuestion> flattenedQuestions)
    {
        flattenedQuestions.Add(question);
        foreach (CultureSurveyQuestion q in question.ChildQuestions)
        {
            FlattenQuestions(q, flattenedQuestions);
        }
    }

}



// *********** NOTE ************
// Since MemberwiseClone makes a shallow copy of the objects in CultureSurveyQuestion (which is needed when making multiple cb Country Specific sections)
// we need to make sure that if we add a NEW non primitive values to the CultureSurveyQuestion object that we add the cooresponding code in ReturnAsDeepClonedCopy
// to create the new non primitive object. See what's being done in ReturnAsDeepClonedCopy for AnswerOptions, AnswerOptionsTextOnly, OnlyAskInCountry and CurrencyInfo
public class CultureSurveyQuestion
    {
        public string DisplayText { get; set; } = "";
        public string Instructions { get; set; } = "";
        public string ListExplanation { get; set; } = "";
        public string FieldType { get; set; } = ""; // TODO - could also be an enum
        public int FieldWidth { get; set; } = 0;
        public string VariableName { get; set; }
        public bool Required { get; set; } = false;
        public bool CanBeConfidential { get; set; } = false;
        public bool CanBePrepopulated { get; set; } = false;
        public bool CanBeGlobal { get; set; } = false;
        public int MaxWordCount { get; set; } = 0;
        public List<CultureSurveyAnswerOption> AnswerOptions { get; set; } = new List<CultureSurveyAnswerOption>();
        public List<String> AnswerOptionsTextOnly { get; set; } = new List<String>();
        public string DependentVariableName { get; set; } = "";
        public List<CultureSurveyQuestion> ChildQuestions { get; set; } = new List<CultureSurveyQuestion>();
        public List<string> OnlyAskInCountry = new List<string>();
        public List<string> DoNotAskInCountry = new List<string>();
        public bool HiddenDataOnlyQuestion { get; set; } = false;
        public CurrencyInfo CurrencyInfo { get; set; } = new CurrencyInfo();
        public List<VisibilityRule> VisibilityRules = new List<VisibilityRule>();
        public bool IsStickyHeader { get; set; } = false;

        public CultureSurveyQuestion()
        {
        }

    // https://stackoverflow.com/questions/129389/how-do-you-do-a-deep-copy-of-an-object-in-net
    public static object DeepCopy(object obj)
    {
        if (obj == null)
            return null;
        Type type = obj.GetType();

        if (type.IsValueType || type == typeof(string))
        {
            return obj;
        }
        else if (type.IsArray)
        {
            Type elementType = Type.GetType(
                 type.FullName.Replace("[]", string.Empty));
            var array = obj as Array;
            Array copied = Array.CreateInstance(elementType, array.Length);
            for (int i = 0; i < array.Length; i++)
            {
                copied.SetValue(DeepCopy(array.GetValue(i)), i);
            }
            return Convert.ChangeType(copied, obj.GetType());
        }
        else if (type.IsClass)
        {

            object toret = Activator.CreateInstance(obj.GetType());
            FieldInfo[] fields = type.GetFields(BindingFlags.Public |
                        BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (FieldInfo field in fields)
            {
                object fieldValue = field.GetValue(obj);
                if (fieldValue == null)
                    continue;
                field.SetValue(toret, DeepCopy(fieldValue));
            }
            return toret;
        }
        else
            throw new ArgumentException("Unknown type");
    }

//    public static CultureSurveyQuestion ReturnAsDeepClonedCopy(CultureSurveyQuestion question, string countryCode)
//    {
//        CultureSurveyQuestion returnedQuestion = (CultureSurveyQuestion)question.MemberwiseClone();
//        returnedQuestion.ChildQuestions = new List<CultureSurveyQuestion>();

//        returnedQuestion.AnswerOptions = new List<CultureSurveyAnswerOption>();
//        foreach (CultureSurveyAnswerOption answerOption in question.AnswerOptions)
//        {
//            returnedQuestion.AnswerOptions.Add(new CultureSurveyAnswerOption
//            {
//                AnswerOptionId = answerOption.AnswerOptionId,
//                DependentValue = answerOption.DependentValue,
//                AnswerOptionText = answerOption.AnswerOptionText,
//                VariableName = answerOption.VariableName
//            });
//        }

//        returnedQuestion.AnswerOptionsTextOnly = new List<String>();
//        foreach (string answerOptionsTextOnly in question.AnswerOptionsTextOnly)
//        {
//            returnedQuestion.AnswerOptionsTextOnly.Add(answerOptionsTextOnly);
//        }

//        returnedQuestion.OnlyAskInCountry = new List<String>();
//        foreach (string OnlyAskInCountry in question.OnlyAskInCountry)
//        {
//            returnedQuestion.OnlyAskInCountry.Add(OnlyAskInCountry);
//        }

//        returnedQuestion.CurrencyInfo = new CurrencyInfo();
//        returnedQuestion.CurrencyInfo.CurrencyCode = question.CurrencyInfo.CurrencyCode;
//        returnedQuestion.CurrencyInfo.CurrencySymbol = question.CurrencyInfo.CurrencySymbol;

//        foreach (CultureSurveyQuestion childQuestion in question.ChildQuestions)
//        {
//            // If there are no countries specified OR the countryCode passed in matches a country then we'll show add this question (and all child questions)
//            if (childQuestion.OnlyAskInCountry.Count == 0 || childQuestion.OnlyAskInCountry.Contains(countryCode))
//            {
//                returnedQuestion.ChildQuestions.Add(ReturnAsDeepClonedCopy(childQuestion, countryCode));
//            }
//        }

//        return returnedQuestion;
//    }

}

public class CultureSurveyAnswerOption
{
    public int AnswerOptionId { get; set; } = 0;
    public string DependentValue { get; set; } = "";
    public string AnswerOptionText { get; set; } = "";
    public string VariableName { get; set; } = "";
    public string AnswerOptionValue { get; set; } = "";

    public CultureSurveyAnswerOption()
    {
    }
}

public class CurrencyInfo
{
    public string CurrencySymbol { get; set; } = "";
    public string CurrencyCode { get; set; } = "";
}

public class VisibilityRule
{
    public string VariableName { get; set; } = "";
    public string Response { get; set; } = "";
    public string Action { get; set; } = "";
}
