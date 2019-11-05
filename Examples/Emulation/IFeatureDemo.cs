using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dasync.Modeling;

namespace DasyncFeatures
{
    public interface IFeatureDemo
    {
        string Name { get; }

        ICommunicationModel Model { get; }

        Dictionary<Type, Type> Bindings { get; }

        Task Run(IServiceProvider services);
    }
}
