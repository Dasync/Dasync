using System;
using System.Collections.Generic;
using System.Linq;

namespace Dasync.Fabric.Sample.Base
{
    public class CurrentFabricHolder : ICurrentFabric, ICurrentFabricSetter
    {
        public IFabric Fabric { get; set; }

        public bool IsAvailable => Fabric != null;

        public IFabric Instance => Fabric ?? throw new InvalidOperationException("Not running a fabric.");

        public void SetInstance(IFabric fabric) => Fabric = fabric;
    }
}
