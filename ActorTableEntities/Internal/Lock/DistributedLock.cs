using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;

namespace ActorTableEntities.Internal.Lock
{
    internal class DistributedLock : IDisposable
    {
        private readonly BlobContainerClient containerClient;
        private readonly string key;
        
        private string leaseId;

        private bool disposed = false;

        public DistributedLock(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException(nameof(key));
            }

            this.key = key;
            this.containerClient = DistributedLockFactory.BlobServiceClient.GetBlobContainerClient(DistributedLockFactory.Settings.ContainerName);
        }

        internal async Task AcquireAsync(ActorTableEntityOptions options = null)
        {
            var blobClient = await GetBlobReference();

            if (!await blobClient.ExistsAsync())
            {
                await blobClient.UploadAsync(BinaryData.FromString(string.Empty));
            }

            var leaseClient = blobClient.GetBlobLeaseClient();

            try
            {
                if (options?.WithRetry == true)
                {
                    var lease = await Do(() => leaseClient.AcquireAsync(TimeSpan.FromMilliseconds(options.RetryIntervalMilliseconds)));
                    leaseId = lease.Value.LeaseId;
                }
                else
                {
                    var lease = await leaseClient.AcquireAsync(TimeSpan.FromSeconds(60));
                    leaseId = lease.Value.LeaseId;
                }
            }
            catch (RequestFailedException ex) when (ex.Status == 409)
            {
                throw new InvalidOperationException($"Another job is already running for {key}.");
            }
        }

        internal async Task ReleaseAsync()
        {
            var blobClient = await GetBlobReference();
            var leaseClient = blobClient.GetBlobLeaseClient(leaseId);

            await leaseClient.ReleaseAsync();
        }

        internal async Task RenewAsync()
        {
            var blobClient = await GetBlobReference();
            var leaseClient = blobClient.GetBlobLeaseClient(leaseId);

            await leaseClient.RenewAsync();
        }

        private async Task<BlobClient> GetBlobReference()
        {
            await containerClient.CreateIfNotExistsAsync();
            return containerClient.GetBlobClient(key);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
            {
                return;
            }

            if (disposing)
            {
                
            }

            disposed = true;
        }

        private async Task<T> Do<T>(
            Func<Task<T>> action,
            int retryInterval = 50,
            int maxAttemptCount = 10)
        {
            var exceptions = new List<Exception>();

            for (int attempted = 0; attempted < maxAttemptCount; attempted++)
            {
                try
                {
                    if (attempted > 0)
                    {
                        await Task.Delay(retryInterval);
                    }
                    return await action();
                }
                catch (RequestFailedException ex) when (ex.Status == 409 || ex.Status == 412)
                {
                    // 409 Conflict: Lease is already held by another client
                    // 412 Precondition Failed: Lease ID mismatch or other lease issues
                    exceptions.Add(ex);
                }
            }

            throw new AggregateException(exceptions);
        }

        ~DistributedLock()
        {
            Dispose(false);
        }
    }
}