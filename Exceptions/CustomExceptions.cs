using System;
using System.Collections.Generic;

namespace DeskGuardBackend.Exceptions
{
    public abstract class BaseException : Exception
    {
        public int StatusCode { get; }
        public IDictionary<string, string[]> Errors { get; }

        protected BaseException(string message, int statusCode = 400, IDictionary<string, string[]>? errors = null) 
            : base(message)
        {
            StatusCode = statusCode;
            Errors = errors ?? new Dictionary<string, string[]>();
        }
    }

    public class UnauthorizedActionException : BaseException
    {
        public UnauthorizedActionException(string message, int statusCode = 401, IDictionary<string, string[]>? errors = null)
            : base(message, statusCode, errors)
        {
        }
    }

    public class MachineNotFoundException : BaseException
    {
        public MachineNotFoundException(string message, int statusCode = 404, IDictionary<string, string[]>? errors = null)
            : base(message, statusCode, errors)
        {
        }
    }

    public class MachineRegistrationException : BaseException
    {
        public MachineRegistrationException(string message, int statusCode = 422, IDictionary<string, string[]>? errors = null)
            : base(message, statusCode, errors)
        {
        }
    }

    public class AlertGenerationException : BaseException
    {
        public AlertGenerationException(string message, int statusCode = 400, IDictionary<string, string[]>? errors = null)
            : base(message, statusCode, errors)
        {
        }
    }

    public class InventorySyncException : BaseException
    {
        public InventorySyncException(string message, int statusCode = 400, IDictionary<string, string[]>? errors = null)
            : base(message, statusCode, errors)
        {
        }
    }

    public class AccountException : BaseException
    {
        public AccountException(string message, int statusCode = 422, IDictionary<string, string[]>? errors = null)
            : base(message, statusCode, errors)
        {
        }
    }
}
