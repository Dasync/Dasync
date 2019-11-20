using System;
using System.Collections.Generic;
using System.Linq;

namespace Dasync.Hosting.AspNetCore.Errors
{
    public static class ExceptionToErrorConverter
    {
        public static Error ToError(this Exception ex)
        {
            var error = new Error
            {
                Code = ex.HResult,
                Type = ex.GetType().Name,
                Message = ex.Message,
                ExtendedHelp = ex.HelpLink,
                StackTrace = ex.StackTrace,
                ExtendedProperties = ex.Data.Count > 0 ? ex.Data : null
            };

            if (error.Type.EndsWith("Exception"))
                error.Type = error.Type.Substring(0, error.Type.Length - 9);

            if (ex is AggregateException aggregateException && aggregateException.InnerExceptions?.Count > 0)
            {
                error.Errors = new List<Error>(aggregateException.InnerExceptions.Select(innerEx => innerEx.ToError()));
            }
            else if (ex.InnerException != null)
            {
                error.Errors = new List<Error> { ex.InnerException.ToError() };
            }

            return error;
        }
    }
}
