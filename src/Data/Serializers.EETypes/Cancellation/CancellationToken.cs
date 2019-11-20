using System;
using System.Threading;
using Dasync.Accessors;
using Dasync.Serialization;
using Dasync.ValueContainer;

namespace Dasync.Serializers.EETypes.Cancellation
{
    public class CancellationTokenSerializer : IObjectDecomposer, IObjectComposer
    {
        public IValueContainer Decompose(object value)
        {
            var cancellationToken = (CancellationToken)value;
            return new CancellationTokenContainer
            {
                IsCanceled = cancellationToken.IsCancellationRequested,
                Source = cancellationToken.IsCancellationRequested ? null : cancellationToken.GetSource()
            };
        }

        public object Compose(IValueContainer container, Type valueType)
        {
            var values = (CancellationTokenContainer)container;

            if (values.IsCanceled)
                return new CancellationToken(canceled: true);

            if (values.Source == null)
                return CancellationToken.None;

            return values.Source.Token;
        }

        public IValueContainer CreatePropertySet(Type valueType)
        {
            return new CancellationTokenContainer();
        }
    }

    public class CancellationTokenContainer : ValueContainerBase
    {
        public bool IsCanceled;
        public CancellationTokenSource Source;
    }
}
