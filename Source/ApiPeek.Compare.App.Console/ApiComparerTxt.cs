using System.Diagnostics;
using System.Text;
using ApiPeek.Core.Model;
using Newtonsoft.Json;

namespace ApiPeek.Compare.App;

internal sealed class ApiComparerTxt
{
    public const string Prefix = "    ";
    public static bool DetailedDetailLog { get; set; }

    private static readonly StringBuilder Sb = new();
    private static string? _result;

    internal static void Compare(string oldFile, string newFile, string diffFile)
    {
        try
        {
            Sb.Clear();
            ApiAssembly oldPackage = GetAssembly(oldFile);
            ApiAssembly newPackage = GetAssembly(newFile);

            ComparePackages(oldPackage, newPackage);
            _result = Sb.ToString();
            if (!string.IsNullOrWhiteSpace(_result))
            {
                File.WriteAllText(diffFile, _result);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    internal static void Compare(string[] oldFiles, string[] newFiles, string diffFile)
    {
        try
        {
            Sb.Clear();
            ApiAssembly oldAssembly = new ApiAssembly();
            oldAssembly.Init();
            ApiAssembly newAssembly = new ApiAssembly();
            newAssembly.Init();

            for (int i = 0; i < oldFiles.Length; i++)
            {
                ApiAssembly oldPackage = GetAssembly(oldFiles[i]);
                ApiAssembly newPackage = GetAssembly(newFiles[i]);
                ImportTypes(oldAssembly, oldPackage);
                ImportTypes(newAssembly, newPackage);
            }

            ComparePackages(oldAssembly, newAssembly);
            _result = Sb.ToString();
            if (!string.IsNullOrWhiteSpace(_result))
            {
                File.WriteAllText(diffFile, _result);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    private static void ComparePackages(ApiAssembly oldAssembly, ApiAssembly newAssembly)
    {
        List<ApiBaseItem> allOld = GetAllItems(oldAssembly);
        ApiNamespace[] oldNamespaces = GetNamespaces(allOld, 0);
        List<ApiBaseItem> allNew = GetAllItems(newAssembly);
        ApiNamespace[] newNamespaces = GetNamespaces(allNew, 0);
        DiffItem[] diffs = CompareItems(oldNamespaces, newNamespaces).ToArray();
        LogDiffs(diffs, "");
    }

    // oldItem and newItem must be of same type, returning same list of items lists
    private static DiffItem CompareItems(ApiBaseItem? oldItem, ApiBaseItem? newItem)
    {
        DiffItem diff = new DiffItem
        {
            DiffType = oldItem == null
                ? DiffType.Added : newItem == null
                    ? DiffType.Removed : DiffType.Changed
        };
        diff.Item = diff.DiffType == DiffType.Removed
            ? oldItem : newItem;


        ApiBaseItem item = oldItem ?? newItem ?? throw new InvalidOperationException();
        if (DetailedDetailLog || item is not IApiType)
        {
            ApiBaseItem[][]? oldItemsItems = oldItem?.ApiItemsItems;
            ApiBaseItem[][]? newItemsItems = newItem?.ApiItemsItems;
            ApiBaseItem[][] items = oldItemsItems ?? newItemsItems ?? throw new InvalidOperationException();

            for (int i = 0; i < items.Length; i++)
            {
                ApiBaseItem[] oldItems = oldItemsItems != null ? oldItemsItems[i] : Array.Empty<ApiBaseItem>();
                ApiBaseItem[] newItems = newItemsItems != null ? newItemsItems[i] : Array.Empty<ApiBaseItem>();
                diff.Children.AddRange(CompareItems(oldItems, newItems));
            }
        }

        return diff;
    }

    private static IEnumerable<DiffItem> CompareItems(ApiBaseItem[] oldItems, ApiBaseItem[] newItems)
    {
        List<DiffItem> diffs = new List<DiffItem>();

        ApiBaseItem[] removedItems = oldItems.Except(newItems, ApiBaseItem.SortNameComparer).ToArray();
        diffs.AddRange(removedItems.Select(i => CompareItems(i, null)).Where(i => i.Any()));

        ApiBaseItem[] addedItems = newItems.Except(oldItems, ApiBaseItem.SortNameComparer).ToArray();
        diffs.AddRange(addedItems.Select(i => CompareItems(null, i)).Where(i => i.Any()));

        ApiBaseItem[] sameOld = oldItems.Except(removedItems, ApiBaseItem.SortNameComparer).ToArray();
        ApiBaseItem[] sameNew = newItems.Except(addedItems, ApiBaseItem.SortNameComparer).ToArray();
        var zip = sameOld.Zip(sameNew, (i1, i2) => new Tuple<ApiBaseItem, ApiBaseItem>(i1, i2)).ToArray();
        diffs.AddRange(zip.Select(i => CompareItems(i.Item1, i.Item2)).Where(i => i.Any()));

        return diffs.OrderBy(d => d.Item.Name);
    }

    private static void LogDiffs(ICollection<DiffItem> diffs, string prefix)
    {
        foreach (DiffItem diff in diffs)
        {
            Log($"{prefix}{diff.TypePrefix}{diff.Item.ShortString}");
            LogDiffs(diff.Children, prefix + Prefix);
        }
    }

    private static void Log(string log)
    {
        //Console.WriteLine(log);
        Sb.AppendLine(log);
    }

    #region Internals

    private static void ImportTypes(ApiAssembly targetAssembly, ApiAssembly sourceAssembly)
    {
        targetAssembly.Classes = targetAssembly.Classes.Concat(sourceAssembly.Classes).DistinctBy(c => c.Name).ToArray();
        targetAssembly.Delegates = targetAssembly.Delegates.Concat(sourceAssembly.Delegates).DistinctBy(c => c.Name).ToArray();
        targetAssembly.Enums = targetAssembly.Enums.Concat(sourceAssembly.Enums).DistinctBy(c => c.Name).ToArray();
        targetAssembly.Interfaces = targetAssembly.Interfaces.Concat(sourceAssembly.Interfaces).DistinctBy(c => c.Name).ToArray();
        targetAssembly.Structs = targetAssembly.Structs.Concat(sourceAssembly.Structs).DistinctBy(c => c.Name).ToArray();
    }

    private static ApiAssembly GetAssembly(string file)
    {
        ApiAssembly package;
        try
        {
            if (File.Exists(file))
            {
                string fileString = File.ReadAllText(file);
                package = JsonConvert.DeserializeObject<ApiAssembly>(fileString) ?? throw new InvalidOperationException();
            }
            else package = new ApiAssembly();
        }
        catch (Exception e)
        {
            package = new ApiAssembly();
            Debug.WriteLine(e);
        }
        package.Init();
        return package;
    }

    private static List<ApiBaseItem> GetAllItems(ApiAssembly assembly)
    {
        return assembly.Classes.Cast<ApiBaseItem>()
            .Concat(assembly.Delegates)
            .Concat(assembly.Enums)
            .Concat(assembly.Interfaces)
            .Concat(assembly.Structs)
            .OrderBy(i => i.Name)
            .ToList();
    }

    private static ApiNamespace GetNamespace(string name, IEnumerable<ApiBaseItem> items, int p)
    {
        IGrouping<bool, ApiBaseItem>[] split = items.GroupBy(i => i.NameSegments.Length == p+1).ToArray();
        // true - end type
        IGrouping<bool, ApiBaseItem>? endTypes = split.FirstOrDefault(g => g.Key);
        // false, more segments - with nested namespace
        IGrouping<bool, ApiBaseItem>? withNestedNs = split.FirstOrDefault(g => !g.Key);
            
        ApiNamespace ns = new ApiNamespace(name);
        if (endTypes != null) ns.ApiItems = endTypes.ToArray();
        if (withNestedNs != null) ns.Namespaces = GetNamespaces(withNestedNs.ToArray(), p+1);
        return ns;
    }

    private static ApiNamespace[] GetNamespaces(ICollection<ApiBaseItem> items, int p)
    {
        ApiNamespace[] namespaces = items
            .GroupBy(i => string.Join(".", i.NameSegments.Take(p)))
            .Select(g => GetNamespace(g.Key, g, p))
            .ToArray();
        return namespaces;
    }

    #endregion
}

internal static class MyExtensions
{
    public static IEnumerable<TSource> DistinctBy<TSource, TKey>
        (this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
    {
        return source.GroupBy(keySelector).Select(g => g.First());
    }
}