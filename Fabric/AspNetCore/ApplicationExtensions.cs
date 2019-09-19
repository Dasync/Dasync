using System;
using System.Collections.Generic;
using Dasync.AspNetCore;
using Dasync.Modeling;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace DasyncAspNetCore
{
    public static class ApplicationExtensions
    {
        public static IApplicationBuilder UseDasync(this IApplicationBuilder app, ICommunicationModel model)
        {
            app.ApplicationServices.GetService<CommunicationModelProvider.Holder>().Model = model;

            app.UseMiddleware<DasyncMiddleware>();

            return app;
        }

        public static IApplicationBuilder MapExceptionToHttpCode(
            this IApplicationBuilder app,
            params (Type exceptionType, int statusCode)[] mapping) =>
            app.MapExceptionToHttpCode((IEnumerable<(Type, int)>)mapping);

        public static IApplicationBuilder MapExceptionToHttpCode(
            this IApplicationBuilder app,
            IEnumerable<(Type exceptionType, int statusCode)> mapping)
        {
            var map = app.ApplicationServices.GetService<HttpStatusCodeExceptionMap>();
            foreach (var (type, code) in mapping)
                map.AddMapping(type, code);
            return app;
        }
    }
}
