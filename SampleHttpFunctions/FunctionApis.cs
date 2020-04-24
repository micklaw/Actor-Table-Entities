using System.Threading.Tasks;
using ActorTableEntities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using SampleHttpFunctions.Entities;

namespace SampleHttpFunctions
{
    public class FunctionApis
    {
        [FunctionName("UpdateHttpApi")]
        public async Task<IActionResult> Update(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "update/{name}")] HttpRequest req, string name,
            [ActorTableEntity] IActorTableEntityClient entityClient)
        {
            await using var state = await entityClient.GetLocked<Counter>("entity", name);

            state.Entity.Increment();

            await state.Flush();

            return new OkObjectResult(state.Entity);
        }

        [FunctionName("GetHttpApi")]
        public async Task<IActionResult> Get(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "get/{name}")] HttpRequest req, string name,
            [ActorTableEntity] IActorTableEntityClient entityClient)
        {
            var counter = await entityClient.Get<Counter>("entity", name);

            return new OkObjectResult(counter);
        }
    }
}
