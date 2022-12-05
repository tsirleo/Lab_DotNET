using System.Net;

namespace Server.Database
{
    class Error : ArgumentException
    {
        public HttpStatusCode Status { get; }
        public Error(string message, HttpStatusCode status)
            : base(message)
        {
            Status = status;
        }
    }
}

