using Microsoft.Azure.WebJobs.Description;

namespace Loom.Azure.Functions.Extensions.Amazon.SQS;

[Binding]
[AttributeUsage(AttributeTargets.Parameter)]
public sealed class SqsTriggerAttribute : Attribute
{
    public SqsTriggerAttribute(
        string accessKeyId,
        string secretAccessKey,
        string queueUrl,
        string? region = null)
    {
        AccessKeyId = accessKeyId;
        SecretAccessKey = secretAccessKey;
        QueueUrl = queueUrl;
        Region = region;
    }

    public string AccessKeyId { get; }

    public string SecretAccessKey { get; }

    public string QueueUrl { get; }

    public string? Region { get; }
}
