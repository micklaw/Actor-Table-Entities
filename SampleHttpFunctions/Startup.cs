using ActorTableEntities;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Hosting;

[assembly: WebJobsStartup(typeof(SampleHttpFunctions.Startup))]
namespace SampleHttpFunctions
{
    public class Startup : IWebJobsStartup
    {
        public void Configure(IWebJobsBuilder builder)
        {
            builder.AddActorTableEntities(options =>
            {
                options.StorageConnectionString = "UseDevelopmentStorage=true";
                options.ContainerName = "entitylocks";
                options.WithRetry = true;
                options.RetryIntervalMilliseconds = 100;
            });
        }
    }
}
