using System.Collections.Generic;

namespace DeskGuardBackend.Exceptions
{
    public class AlertProfileException : BaseException
    {
        public AlertProfileException(string message, int statusCode = 422, IDictionary<string, string[]>? errors = null)
            : base(message, statusCode, errors)
        {
        }
    }
}
