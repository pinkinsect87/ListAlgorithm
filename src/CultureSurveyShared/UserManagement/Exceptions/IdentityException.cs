using System;

namespace CultureSurveyShared.UserManagement.Exceptions
{
    [Serializable]
    public class IdentityException : Exception
    {
        public IdentityException() { }
        public IdentityException(string message) : base(message) { }
        public IdentityException(string message, Exception inner) : base(message, inner) { }
        protected IdentityException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
