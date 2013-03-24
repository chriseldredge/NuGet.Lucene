using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace NuGet.Lucene.Util
{
    public static class CollectionExtensions
    {
        /// <summary>
        /// Waiting up to <paramref name="firstItemTimeout">firstItemTimeout</paramref>
        /// for the first item to become available, then return all other items that
        /// are immediately available.
        /// </summary>
        public static void TakeAvailable<T>(this BlockingCollection<T> collection, IList<T> destination, TimeSpan firstItemTimeout)
        {
            T item;
            var timeout = firstItemTimeout;
            while (collection.TryTake(out item, timeout))
            {
                destination.Add(item);
                timeout = TimeSpan.Zero;
            }
        }

        public static IEnumerable<T> WithCancellation<T>(this IEnumerable<T> enumerable, CancellationToken cancellation)
        {
            foreach (var item in enumerable)
            {
                cancellation.ThrowIfCancellationRequested();
                yield return item;
            }
        }
    }
}