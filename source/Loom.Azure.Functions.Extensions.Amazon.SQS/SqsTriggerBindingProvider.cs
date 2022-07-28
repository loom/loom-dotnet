using System.Reflection;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host.Triggers;

namespace Loom.Azure.Functions.Extensions.Amazon.SQS;

internal class SqsTriggerBindingProvider : ITriggerBindingProvider
{
    private readonly INameResolver _nameResolver;

    public SqsTriggerBindingProvider(INameResolver nameResolver)
    {
        _nameResolver = nameResolver;
    }

    public Task<ITriggerBinding?> TryCreateAsync(
        TriggerBindingProviderContext context)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        ITriggerBinding? binding = CreateBinding(context);
        return Task.FromResult(binding);
    }

    public ITriggerBinding? CreateBinding(TriggerBindingProviderContext context)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        ParameterInfo parameter = context.Parameter;

        SqsTriggerAttribute? attribute = parameter
            .GetCustomAttributes(inherit: default)
            .OfType<SqsTriggerAttribute>()
            .FirstOrDefault();

        if (attribute == null)
        {
            return null;
        }

        return new SqsTriggerBinding(
            GetValue(attribute.AccessKeyId),
            GetValue(attribute.SecretAccessKey),
            TryGetValue(attribute.Region),
            GetValue(attribute.QueueUrl));
    }

    private string GetValue(string name)
        => _nameResolver.Resolve(name) ?? name;

    private string? TryGetValue(string? name)
        => name == null ? null : GetValue(name);
}
