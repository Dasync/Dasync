using System;

namespace Dasync.Modeling
{
    internal class EntityProjectionDefinition : PropertyBag, IMutableEntityProjectionDefinition, IEntityProjectionDefinition, IMutablePropertyBag, IPropertyBag
    {
        public EntityProjectionDefinition(CommunicationModel model, Type interfaceType)
        {
            Model = model;
            InterfaceType = interfaceType;

            Model.OnEntityProjectionInterfaceSet(this);
        }

        public CommunicationModel Model { get; }

        public Type InterfaceType { get; }

        ICommunicationModel IEntityProjectionDefinition.Model => Model;

        IMutableCommunicationModel IMutableEntityProjectionDefinition.Model => Model;
    }
}
