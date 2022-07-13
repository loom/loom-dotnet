using System.Collections.Immutable;
using Amazon.SQS.Model;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Azure.WebJobs.Host.Listeners;
using Microsoft.Azure.WebJobs.Host.Protocols;
using Microsoft.Azure.WebJobs.Host.Triggers;

namespace Loom.Azure.Functions.Extensions.Amazon.SQS;

internal class SqsTriggerBinding : ITriggerBinding
{
    private readonly string _accessKeyId;
    private readonly string _secretAccessKey;
    private readonly string _queueUrl;

    public SqsTriggerBinding(
        string accessKeyId,
        string secretAccessKey,
        string queueUrl)
    {
        _accessKeyId = accessKeyId;
        _secretAccessKey = secretAccessKey;
        _queueUrl = queueUrl;
    }

    public Type TriggerValueType => typeof(Message);

    public IReadOnlyDictionary<string, Type> BindingDataContract
        => ImmutableDictionary.Create<string, Type>();

    public Task<ITriggerData> BindAsync(
        object value,
        ValueBindingContext context)
    {
        return Task.FromResult<ITriggerData>(
            new TriggerData(
                new SqsTriggerValueProvider(value),
                bindingData: ImmutableDictionary.Create<string, object>()));
    }

    public Task<IListener> CreateListenerAsync(ListenerFactoryContext context)
    {
        SqsListener listener = new(
            _accessKeyId,
            _secretAccessKey,
            _queueUrl,
            context.Executor);

        return Task.FromResult<IListener>(listener);
    }

    public ParameterDescriptor? ToParameterDescriptor() => null!;
}
