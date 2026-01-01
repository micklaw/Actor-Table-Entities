using System;
using Azure.Storage.Blobs;

namespace ActorTableEntities.Internal.Lock
{
    internal class DistributedLockFactory
    {

        public static ActorTableEntityOptions Settings { get; private set; }

        public static BlobServiceClient BlobServiceClient { get; private set; }

        public static void Initialise(ActorTableEntityOptions options)
        {
            Settings = options;

            if (Settings == null)
            {
                return;
            }

            if (Settings.StorageConnectionString == null)
            {
                throw new ArgumentNullException(nameof(Settings.StorageConnectionString));
            }

            if (Settings.ContainerName == null)
            {
                throw new ArgumentNullException(nameof(Settings.ContainerName));
            }

            BlobServiceClient = new BlobServiceClient(Settings.StorageConnectionString);
        }

        public static DistributedLock Get(string key) => new DistributedLock(key);
    }
}
