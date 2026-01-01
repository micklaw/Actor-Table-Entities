using ActorTableEntities;

namespace SampleHttpFunctions.Entities;

public class Counter : ActorTableEntity
{
    public int Count { get; set; }

    public Counter Increment()
    {
        Count = Count + 1;

        return this;
    }
}
