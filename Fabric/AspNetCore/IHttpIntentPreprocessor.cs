using System.Collections.Generic;
using System.Threading.Tasks;
using Dasync.EETypes;
using Dasync.Modeling;
using Dasync.ValueContainer;
using Microsoft.AspNetCore.Http;

namespace Dasync.AspNetCore
{
    public interface IHttpIntentPreprocessor
    {
        void PrepareContext(HttpContext context);

        ValueTask<bool> PreprocessAsync(HttpContext context, IServiceDefinition serviceDefinition, MethodId methodId, IValueContainer parameters);
    }

    internal class AggregateHttpIntentPreprocessor : IHttpIntentPreprocessor
    {
        private readonly IEnumerable<IHttpIntentPreprocessor> _intentPreprocessors;

        public AggregateHttpIntentPreprocessor(IEnumerable<IHttpIntentPreprocessor> intentPreprocessors) =>
            _intentPreprocessors = intentPreprocessors;

        public void PrepareContext(HttpContext context)
        {
            foreach (var preprocessor in _intentPreprocessors)
                preprocessor.PrepareContext(context);
        }

        public async ValueTask<bool> PreprocessAsync(HttpContext context, IServiceDefinition serviceDefinition, MethodId methodId, IValueContainer parameters)
        {
            if (_intentPreprocessors == null)
                return false;

            foreach (var preprocessor in _intentPreprocessors)
                if (await preprocessor.PreprocessAsync(context, serviceDefinition, methodId, parameters))
                    return true;

            return false;
        }
    }
}
