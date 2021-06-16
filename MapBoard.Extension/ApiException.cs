using System;

namespace MapBoard.Extension
{
    public class ApiException : Exception
    {
        public ApiException(string message) : base(message)
        {
        }

        public ApiException() : base()
        {
        }
    }
}