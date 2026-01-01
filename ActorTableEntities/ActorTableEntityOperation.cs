using Azure.Data.Tables;

namespace ActorTableEntities
{
    public class ActorTableEntityOperation
    {
        public ITableEntity Entity { get; set; }

        public ActorTableEntityOperation(ITableEntity entity)
        {
            Entity = entity;
        }
    }
}
