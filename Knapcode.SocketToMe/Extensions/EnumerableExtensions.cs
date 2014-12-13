using System.Collections.Generic;
using System.Linq;

namespace Knapcode.SocketToMe.Extensions
{
    public static class EnumerableExtensions
    {
        /// <summary>
        /// Source: http://stackoverflow.com/a/419063/52749
        /// </summary>
        /// <typeparam name="T">The type of the elements of source.</typeparam>
        /// <param name="source">A sequence of values to break into chunks.</param>
        /// <param name="chunkSize">The chunk size.</param>
        /// <returns>The chunks.</returns>
        public static IEnumerable<IEnumerable<T>> Chunk<T>(this IEnumerable<T> source, int chunkSize)
        {
            return source
                .Select((x, i) => new { Index = i, Value = x })
                .GroupBy(x => x.Index / chunkSize)
                .Select(x => x.Select(v => v.Value).ToList())
                .ToList();
        }
    }
}
