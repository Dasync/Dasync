namespace Dasync.Projections
{
    public static class ObjectExtensions
    {
        public static TInterface Project<TInterface>(this object entity) where TInterface : class
        {
            if (entity == null)
                return default;
            var projection = Projection.CreateInstance<TInterface>();
            var projectionType = projection.GetType();
            var entityType = entity.GetType();
            foreach (var pi in typeof(TInterface).GetProperties())
            {
                projectionType.GetProperty(pi.Name).SetValue(
                    projection, entityType.GetProperty(pi.Name).GetValue(entity));
            }
            return projection;
        }
    }
}
