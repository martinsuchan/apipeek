using System.Collections.Generic;
using System.Linq;

namespace ApiPeek.Core.Extensions;

internal static class EnumerableExtensions
{
    public static bool IsNullOrEmpty<TSource>(this ICollection<TSource> collection)
    {
        return collection == null || !collection.Any();
    }

    public static bool CollectionEquals<TSource>(this ICollection<TSource> old, ICollection<TSource> @new)
    {
        return old.Count == @new.Count && old.Intersect(@new).Count() == old.Count;
    }
}