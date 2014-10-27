using System;

namespace NuGet.Lucene
{
    public class InvalidSearchCriteriaException : Exception
    {
        public InvalidSearchCriteriaException(string message) : base(message)
        {
        }

        public InvalidSearchCriteriaException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
