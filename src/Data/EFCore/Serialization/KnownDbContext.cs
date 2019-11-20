using System;
using System.Collections.Generic;
using Dasync.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Dasync.EntityFrameworkCore.Serialization
{
    public class KnownDbContextTypes
    {
        public KnownDbContextTypes(IEnumerable<Type> types) =>
            Types = new HashSet<Type>(types);

        public IReadOnlyCollection<Type> Types { get; }
    }

    public interface IKnownDbContextSet
    {
        IReadOnlyDictionary<Type, IModel> TypesAndModels { get; }
    }

    public class KnownDbContextSet : IKnownDbContextSet
    {
        private readonly KnownDbContextTypes _knownDbContextTypes;
        private Dictionary<Type, IModel> _typesAndModels;
        private readonly object _initializeLock = new object();
        private readonly IServiceProviderScope _serviceProviderScope;

        public KnownDbContextSet(KnownDbContextTypes knownDbContextTypes, IServiceProviderScope serviceProviderScope)
        {
            _knownDbContextTypes = knownDbContextTypes;
            _serviceProviderScope = serviceProviderScope;
        }

        public IReadOnlyDictionary<Type, IModel> TypesAndModels
        {
            get
            {
                SafeInitialize();
                return _typesAndModels;
            }
        }

        private void SafeInitialize()
        {
            if (_typesAndModels != null)
                return;

            lock (_initializeLock)
            {
                if (_typesAndModels != null)
                    return;

                Initialize();
            }
        }

        private void Initialize()
        {
            var typesAndModels = new Dictionary<Type, IModel>();

            using (var scope = _serviceProviderScope.New())
            {
                foreach (var dbContextType in _knownDbContextTypes.Types)
                {
                    var dbContext = (DbContext)scope.ServiceProvider.GetService(dbContextType);
                    var model = dbContext.Model;
                    typesAndModels.Add(dbContextType, model);
                }
            }

            _typesAndModels = typesAndModels;
        }
    }
}
