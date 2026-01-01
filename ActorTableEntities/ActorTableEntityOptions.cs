namespace ActorTableEntities
{
    public class ActorTableEntityOptions
    {
        public string StorageConnectionString { get; set; }

        public string ContainerName { get; set;  }

        public string StateContainerName { get; set; }

        public bool WithRetry { get; set; }

        public int RetryIntervalMilliseconds { get; set; }

        public ActorTableEntityOptions(string storageConnectionString = null, string containerName = null, string stateContainerName = null, bool withRetry = false, int retryIntervalMilliseconds = 50)
        {
            StorageConnectionString = storageConnectionString;

            ContainerName = containerName;

            StateContainerName = stateContainerName;

            WithRetry = withRetry;

            RetryIntervalMilliseconds = retryIntervalMilliseconds;
        }
    }
}
