using ActorTableEntities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using SampleHttpFunctions.Entities;

namespace SampleHttpFunctions;

public class FunctionApis
{
    private readonly IActorTableEntityClient _entityClient;

    public FunctionApis(IActorTableEntityClient entityClient)
    {
        _entityClient = entityClient;
    }

    [Function("UpdateHttpApi")]
    public async Task<IActionResult> Update(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "update/{name}")] HttpRequest req, 
        string name)
    {
        await using var state = await _entityClient.GetLocked<Counter>("entity", name);

        state.Entity.Increment();

        await state.Flush();

        return new OkObjectResult(state.Entity);
    }

    [Function("GetHttpApi")]
    public async Task<IActionResult> Get(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "get/{name}")] HttpRequest req, 
        string name)
    {
        var counter = await _entityClient.Get<Counter>("entity", name);

        return new OkObjectResult(counter);
    }
}
