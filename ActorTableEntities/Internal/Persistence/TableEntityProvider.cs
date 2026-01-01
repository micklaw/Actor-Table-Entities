using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ActorTableEntities.Internal.Persistence.Extensions;
using ActorTableEntities.Internal.Persistence.Models;
using Azure;
using Azure.Data.Tables;

namespace ActorTableEntities.Internal.Persistence
{
    internal class TableEntityProvider
    {
        private readonly TableStorageProvider storageProvider;

        public TableEntityProvider(TableStorageProvider storageProvider)
        {
            this.storageProvider = storageProvider;
        }

        public Task<PersistResponse<T>> InsertOrReplace<T>(T entities) where T : ITableEntity
        {
            return this.Response<T>(provider => provider.InsertOrReplace(entities));
        }

        public Task<PersistResponse<T>> Get<T>(string partitionKey, string rowKey) where T : class, ITableEntity, new()
        {
            return this.Response<T>(provider => provider.Get<T>(ToKey(partitionKey), ToKey(rowKey)));
        }

        public string ToKey(string value)
        {
            Regex disallowedTableKeysChars = new Regex(@"[\\\\#%+/?\u0000-\u001F\u007F-\u009F]");

            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException(nameof(value));
            }

            return disallowedTableKeysChars.Replace(value, string.Empty);
        }

        private async Task<PersistResponse<T>> Response<T>(Func<TableStorageProvider, Task<Response>> resultAction) where T : ITableEntity
        {
            try
            {
                var result = await resultAction(this.storageProvider);

                return ToPersistResponseOfType<T>(result);
            }
            catch (RequestFailedException exception)
            {
                return ToPersistResponseOfType<T>(exception);
            }
        }

        private async Task<PersistResponse<T>> Response<T>(Func<TableStorageProvider, Task<Response<T>>> resultAction) where T : class, ITableEntity, new()
        {
            try
            {
                var result = await resultAction(this.storageProvider);

                return ToPersistResponseOfType<T>(result.GetRawResponse(), result.Value);
            }
            catch (RequestFailedException exception)
            {
                return ToPersistResponseOfType<T>(exception);
            }
        }

        private PersistResponse<T> ToPersistResponseOfType<T>(Response result, T entity = default) where T : ITableEntity
        {
            result.CheckNotNull(nameof(result));

            return new PersistResponse<T>()
            {
                Message = result.IsError ? "Failed" : "OK",
                Result = entity,
                StatusCode = result.Status,
                ETag = result.Headers.ETag?.ToString()
            };
        }

        private PersistResponse<T> ToPersistResponseOfType<T>(RequestFailedException exception) where T : ITableEntity
        {
            return new PersistResponse<T>()
            {
                Message = $"{exception.ErrorCode}: {exception.Message}",
                StatusCode = exception.Status,
                ETag = null
            };
        }
    }
}
