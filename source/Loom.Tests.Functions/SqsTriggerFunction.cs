using Amazon.SQS.Model;
using Loom.Azure.Functions.Extensions.Amazon.SQS;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace Loom.Tests.Functions;

public class SqsTriggerFunction
{
    [FunctionName(nameof(SqsTriggerFunction))]
    public void Run(
        [SqsTrigger(
            accessKeyId: "Amazon:SQS:AccessKeyId",
            secretAccessKey: "Amazon:SQS:SecretAccessId",
            region: "ap-northeast-2",
            queueUrl: "https://sqs.ap-northeast-2.amazonaws.com/151869951925/test-member-event-queue")]
        Message message,
        ILogger logger)
    {
        logger.LogInformation(message.Body);
    }
}
