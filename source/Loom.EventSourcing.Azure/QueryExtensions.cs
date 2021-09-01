namespace Loom.EventSourcing.Azure
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Cosmos.Table;
    using Microsoft.Azure.Cosmos.Table.Queryable;

    internal static class QueryExtensions
    {
        public static async Task<IEnumerable<TElement>> ExecuteAsync<TElement>(
            this IQueryable<TElement> query,
            CancellationToken cancellationToken)
            where TElement : ITableEntity, new()
        {
            var results = new List<TElement>();

            TableContinuationToken? continuation = default;
            do
            {
                TableQuerySegment<TElement> segment = await query
                    .AsTableQuery()
                    .ExecuteSegmentedAsync(continuation, cancellationToken)
                    .ConfigureAwait(continueOnCapturedContext: false);
                results.AddRange(segment);
                continuation = segment.ContinuationToken;
            }
            while (continuation != default);

            return results.ToList();
        }

        public static async Task ForEach<TElement>(
            this IQueryable<TElement> query, Func<TElement, Task> action)
            where TElement : ITableEntity, new()
        {
            TableContinuationToken? continuation = default;
            do
            {
                TableQuerySegment<TElement> segment = await query
                    .AsTableQuery()
                    .ExecuteSegmentedAsync(continuation)
                    .ConfigureAwait(continueOnCapturedContext: false);

                foreach (TElement element in segment)
                {
                    await action.Invoke(element).ConfigureAwait(continueOnCapturedContext: false);
                }

                continuation = segment.ContinuationToken;
            }
            while (continuation != default);
        }
    }
}
