namespace Dasync.ServiceRegistry
{
    public struct ServiceRegistrationInfo
    {
        public string Name { get; set; }

        public string QualifiedServiceTypeName { get; set; }

        public string QualifiedImplementationTypeName { get; set; }

        public bool IsExternal { get; set; }

        public bool IsSingleton { get; set; }

        public string ConnectorType { get; set; }

        public object ConnectorConfiguration { get; set; }
    }
}
