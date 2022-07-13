using Loom.Azure.Functions.Extensions.Amazon.SQS;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Hosting;

[assembly: WebJobsStartup(typeof(SqsTriggerStartup))]

namespace Loom.Azure.Functions.Extensions.Amazon.SQS;

public sealed class SqsTriggerStartup : IWebJobsStartup
{
    public void Configure(IWebJobsBuilder builder)
        => builder.AddExtension<SqsTriggerExtensionConfigProvider>();
}
