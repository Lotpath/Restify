using System;

namespace Restify
{
    public class RestifyException : Exception
    {
        public RestifyException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}