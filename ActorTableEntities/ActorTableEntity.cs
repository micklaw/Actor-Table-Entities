using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Azure;
using Azure.Data.Tables;
using Newtonsoft.Json;

namespace ActorTableEntities
{
    public abstract class ActorTableEntity : ITableEntity
    {
        private static readonly ConcurrentDictionary<Type, PropertyInfo[]> Cache = new ConcurrentDictionary<Type, PropertyInfo[]>();

        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        protected ActorTableEntity()
        {
        }

        protected ActorTableEntity(string partitionKey, string rowKey)
        {
            PartitionKey = partitionKey;
            RowKey = rowKey;
        }

        private PropertyInfo[] GetComplexProperties()
        {
            var type = this.GetType();

            Cache.TryGetValue(type, out PropertyInfo[] properties);

            if (properties == null)
            {
                properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(i => i.GetCustomAttribute<ActorTableEntityComplexPropertyAttribute>() != null)
                    .ToArray();

                Cache.TryAdd(type, properties);
            }

            return properties;
        }
    }
}
