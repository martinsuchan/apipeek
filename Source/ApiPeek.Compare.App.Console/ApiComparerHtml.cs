using System.Diagnostics;
using System.Text;
using ApiPeek.Core.Model;
using Newtonsoft.Json;

namespace ApiPeek.Compare.App;

internal sealed class ApiComparerHtml
{
    public const string Prefix = "    ";
    public static bool DetailedRemovedLog { get; set; }
    public static bool DetailedDetailLog { get; set; }

    private static readonly StringBuilder Sb = new();
    private static string? _result;
    private static string _title = "";

    internal static void Compare(string[] oldFiles, string[] newFiles, string diffFile, string diffTitle)
    {
        _title = diffTitle;
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
        ApiNamespace[] oldNamespaces = GetNamespaces(allOld, 1);
        List<ApiBaseItem> allNew = GetAllItems(newAssembly);
        ApiNamespace[] newNamespaces = GetNamespaces(allNew, 1);
        List<DiffItem> diffs = CompareItems(oldNamespaces, newNamespaces).ToList();
        DiffItem parentDiff = new DiffItem {DiffType = DiffType.Changed, Children = diffs, Item = new ApiRoot()};
        LogDiffs(parentDiff, "");
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

        return diffs.OrderBy(d => d.Item.SortName);
    }

    private static void LogDiffs(DiffItem parentDiff, string prefix)
    {
        IWriter? writer = GetWriter(parentDiff);
        if (writer == null) return;

        Log($"{prefix}{writer.GetPrefix()}");
        if (writer.Expandable)
        {
            foreach (DiffItem diff in parentDiff.Children)
            {
                LogDiffs(diff, prefix + Prefix);
            }
        }
        Log($"{prefix}{writer.GetSuffix()}");
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

    private static List<ApiBaseItem> GetAllItems(ApiAssembly olda)
    {
        return olda.Classes.Cast<ApiBaseItem>()
            .Concat(olda.Delegates)
            .Concat(olda.Enums)
            .Concat(olda.Interfaces)
            .Concat(olda.Structs)
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

    private static IWriter? GetWriter(DiffItem diffItem)
    {
        if (diffItem.Item is ApiRoot) return new ApiRootWriter(_title);
        if (diffItem.Item is ApiNamespace) return new ApiNamespaceWriter(diffItem);
        if (diffItem.Item is IApiType)
        {
            bool logDetail = DetailedDetailLog && (DetailedRemovedLog || diffItem.DiffType != DiffType.Removed) && diffItem.Children.Any();
            return new ApiTypeWriter(diffItem, logDetail);
        }
        if (diffItem.Item is IDetail) return new ApiMemberWriter(diffItem);
        return null;
    }
}

public interface IWriter
{
    bool Expandable { get; }
    string GetPrefix();
    string GetSuffix();
}

public class ApiRootWriter : IWriter
{
    private readonly string _title;

    public ApiRootWriter(string title)
    {
        _title = title;
    }

    public bool Expandable => true;

    public string GetPrefix()
    {
        string fileString = File.ReadAllText("html\\root.prefix.html");
        return fileString.Replace("==title==", _title);
    }

    public string GetSuffix()
    {
        string fileString = File.ReadAllText("html\\root.suffix.html");
        return fileString;
    }
}

public class ApiNamespaceWriter : IWriter
{
    private readonly ApiNamespace _ns;
    private readonly DiffItem _item;

    public ApiNamespaceWriter(DiffItem item)
    {
        _item = item;
        _ns = (item.Item as ApiNamespace)!;
    }

    public bool Expandable => true;

    public string GetPrefix()
    {
        string id = _ns.Name.ToHash();
        string typeImg = _item.DiffType == DiffType.Added ? "added" : _item.DiffType == DiffType.Removed ? "removed" : "changed";
        return $"<li><img class=\"type\" src=\"{typeImg}.png\" alt=\"{typeImg}\"/><img src=\"namespace.gif\" alt=\"namespace\"/><label for=\"{id}\">{_ns.ShortName}</label> <input checked type=\"checkbox\" id=\"{id}\" /><ol>";
    }

    public string GetSuffix()
    {
        return "</ol></li>";
    }
}

public class ApiTypeWriter : IWriter
{
    private readonly IApiType _api;
    private readonly DiffItem _item;

    public ApiTypeWriter(DiffItem item, bool expandable)
    {
        _item = item;
        Expandable = expandable;
        _api = (item.Item as IApiType)!;
    }

    public bool Expandable { get; }

    public string GetPrefix()
    {
        string changeImg = _item.DiffType == DiffType.Added ? "added" : _item.DiffType == DiffType.Removed ? "removed" : "changed";
        string typeImg = GetTypeImg();
        string safeShortName = _api.ShortName.Esc();
        if (Expandable)
        {
            string id = _api.Name.ToHash();
            return $"<li class=\"file\"><img class=\"type\" src=\"{changeImg}.png\" alt=\"{changeImg}\" /><img src=\"{typeImg}.gif\" alt=\"{typeImg}\"/><label for=\"{id}\">{safeShortName}</label> <input checked type=\"checkbox\" id=\"{id}\" /><ol>";
        }
        else
        {
            return $"<li class=\"file\"><img class=\"type\" src=\"{changeImg}.png\" alt=\"{changeImg}\"/><img src=\"{typeImg}.gif\" alt=\"{typeImg}\"/><p>{safeShortName}</p><input/>";
        }
    }

    private string GetTypeImg()
    {
        if (_api is ApiClass) return "class";
        if (_api is ApiInterface) return "interface";
        if (_api is ApiStruct) return "struct";
        if (_api is ApiEnum) return "enum";
        if (_api is ApiDelegate) return "delegate";
        return string.Empty;
    }

    public string GetSuffix()
    {
        return Expandable ? "</ol></li>" : "</li>";
    }
}

public class ApiMemberWriter : IWriter
{
    private readonly IDetail _detail;
    private readonly DiffItem _item;

    public ApiMemberWriter(DiffItem item)
    {
        _item = item;
        _detail = (item.Item as IDetail)!;
    }

    public bool Expandable => false;

    public string GetPrefix()
    {
        string changeImg = _item.DiffType == DiffType.Added ? "added" : _item.DiffType == DiffType.Removed ? "removed" : "changed";
        string typeImg = GetMemberImg();
        string safeShortString = _detail.ShortString.Esc();
        return $"<li class=\"file\"><img class=\"type\" src=\"{changeImg}.png\" alt=\"{changeImg}\"/><img src=\"{typeImg}.gif\" alt=\"{typeImg}\"/><p>{safeShortString}</p><input/>";
    }

    private string GetMemberImg()
    {
        if (_detail is ApiConstructor) return "method";
        if (_detail is ApiProperty) return "property";
        if (_detail is ApiField) return "field";
        if (_detail is ApiMethod) return "method";
        if (_detail is ApiEvent) return "event";
        if (_detail is ApiEnumValue) return "enum";
        return string.Empty;
    }

    public string GetSuffix()
    {
        return "</li>";
    }
}

public static class UriEscaper
{
    public static string Esc(this string input)
    {
        return input.Replace("<", "&lt;").Replace(">", "&gt;");
    }
}