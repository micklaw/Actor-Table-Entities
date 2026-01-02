namespace ActorTableEntities
{
    public class ActorTableEntityOptions
    {
        public required string StorageConnectionString { get; set; }

        public string ContainerName { get; set;  } = "entitylocks";

        public string StateContainerName { get; set; } = "entitystate";

        public bool WithRetry { get; set; } = true;

        public int RetryIntervalMilliseconds { get; set; } = 100;
    }
}
