using MongoDB.Bson;

    public class CultureSurveyResponse
    {

        public string VariableName { get; set; } = "";
        public string Response { get; set; } = "";
        public bool IsConfidential { get; set; } = false;
        public bool IsValid { get; set; } = false;

        public CultureSurveyResponse( string _variableName, string _Response, bool _IsConfidential)
        {
            VariableName = _variableName;
            Response = _Response;
            IsConfidential = _IsConfidential;
            IsValid = true;
        }

}
