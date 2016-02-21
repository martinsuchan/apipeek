using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using ApiPeek.Core.Model;
using Newtonsoft.Json;

namespace ApiPeek.Compare.App
{
    internal sealed class ApiComparerHtml
    {
        public const string Prefix = "    ";
        public static bool DetailedRemovedLog { get; set; }
        public static bool DetailedDetailLog { get; set; }

        private static readonly StringBuilder sb = new StringBuilder();
        private static string result;
        private static string diffTitle;

        internal static void Compare(string[] oldFiles, string[] newFiles, string diffFile, string _diffTitle)
        {
            diffTitle = _diffTitle;
            try
            {
                sb.Clear();
                ApiAssembly oldp = new ApiAssembly();
                oldp.Init();
                ApiAssembly newp = new ApiAssembly();
                newp.Init();

                for (int i = 0; i < oldFiles.Length; i++)
                {
                    ApiAssembly oldPackage = GetAssembly(oldFiles[i]);
                    ApiAssembly newPackage = GetAssembly(newFiles[i]);
                    ImportTypes(oldp, oldPackage);
                    ImportTypes(newp, newPackage);
                }

                ComparePackages(oldp, newp);
                result = sb.ToString();
                if (!string.IsNullOrWhiteSpace(result))
                {
                    File.WriteAllText(diffFile, result);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private static void ComparePackages(ApiAssembly olda, ApiAssembly newa)
        {
            List<ApiBaseItem> allOld = GetAllItems(olda);
            ApiNamespace[] oldNss = GetNamespaces(allOld, 0);
            List<ApiBaseItem> allNew = GetAllItems(newa);
            ApiNamespace[] newNss = GetNamespaces(allNew, 0);
            List<DiffItem> diffs = CompareItems(oldNss, newNss).ToList();
            DiffItem parentDiff = new DiffItem {DiffType = DiffType.Changed, Children = diffs, Item = new ApiRoot()};
            LogDiffs(parentDiff, "");
        }

        // olditem and newitem must be of same type, returning same list of items lists
        private static DiffItem CompareItems(ApiBaseItem oldItem, ApiBaseItem newItem)
        {
            DiffItem diff = new DiffItem();
            diff.DiffType = oldItem == null
                ? DiffType.Added : newItem == null
                    ? DiffType.Removed : DiffType.Changed;
            diff.Item = diff.DiffType == DiffType.Removed
                ? oldItem : newItem;


            ApiBaseItem item = oldItem ?? newItem;
            if (DetailedDetailLog || !(item is IApiType))
            {
                ApiBaseItem[][] oldItemsItems = oldItem?.ApiItemsItems;
                ApiBaseItem[][] newItemsItems = newItem?.ApiItemsItems;
                ApiBaseItem[][] items = oldItemsItems ?? newItemsItems;

                for (int i = 0; i < items.Length; i++)
                {
                    ApiBaseItem[] oldi = oldItemsItems != null ? oldItemsItems[i] : new ApiBaseItem[0];
                    ApiBaseItem[] newi = newItemsItems != null ? newItemsItems[i] : new ApiBaseItem[0];
                    diff.Children.AddRange(CompareItems(oldi, newi));
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
            IWriter writer = GetWriter(parentDiff);
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
            sb.AppendLine(log);
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
                    package = JsonConvert.DeserializeObject<ApiAssembly>(fileString);
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

        private static ApiNamespace GetNamespace(string groupName, IEnumerable<ApiBaseItem> items, int p)
        {
            IGrouping<bool, ApiBaseItem>[] split = items.GroupBy(i => i.NameSegments.Length-1 == p).ToArray();
            // true - end type
            IGrouping<bool, ApiBaseItem> endTypes = split.FirstOrDefault(g => g.Key);
            // false, more segments - with nested namespace
            IGrouping<bool, ApiBaseItem> withNestedNs = split.FirstOrDefault(g => !g.Key);

            ApiNamespace ns = new ApiNamespace { Name = groupName };
            if (endTypes != null) ns.ApiItems = endTypes.ToArray();
            if (withNestedNs != null) ns.Namespaces = GetNamespaces(withNestedNs.ToArray(), p);
            return ns;
        }

        private static ApiNamespace[] GetNamespaces(ICollection<ApiBaseItem> items, int p)
        {
            ApiNamespace[] namespaces = items
                .GroupBy(i => i.NameSegments[p])
                .Select(g => GetNamespace(g.Key, g, p+1))
                .ToArray();
            return namespaces;
        }

        #endregion

        private static IWriter GetWriter(DiffItem diffItem)
        {
            if (diffItem.Item is ApiRoot) return new ApiRootWriter(diffItem, diffTitle);
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
        private ApiNamespace api;
        private DiffItem item;
        private readonly string title;

        public ApiRootWriter(DiffItem item, string title)
        {
            this.item = item;
            this.title = title;
            this.api = item.Item as ApiNamespace;
        }

        public bool Expandable => true;

        public string GetPrefix()
        {
            string fileString = File.ReadAllText("html\\root.prefix.html");
            return fileString.Replace("==title==", title);
        }

        public string GetSuffix()
        {
            string fileString = File.ReadAllText("html\\root.suffix.html");
            return fileString;
        }
    }

    public class ApiNamespaceWriter : IWriter
    {
        private readonly ApiNamespace api;
        private readonly DiffItem item;

        public ApiNamespaceWriter(DiffItem item)
        {
            this.item = item;
            this.api = item.Item as ApiNamespace;
        }

        public bool Expandable => true;

        public string GetPrefix()
        {
            string id = "id" + Guid.NewGuid().ToString("N");
            string typeImg = item.DiffType == DiffType.Added ? "added" : item.DiffType == DiffType.Removed ? "removed" : "changed";
            return string.Format("<li><img class=\"type\" src=\"{2}.png\"/><img src=\"namespace.gif\"/><label for=\"{0}\">{1}</label> <input checked type=\"checkbox\" id=\"{0}\" /><ol>", id, api.ShortString, typeImg);
        }

        public string GetSuffix()
        {
            return "</ol></li>";
        }
    }

    public class ApiTypeWriter : IWriter
    {
        private readonly IApiType api;
        private readonly DiffItem item;

        public ApiTypeWriter(DiffItem item, bool expandable)
        {
            this.item = item;
            Expandable = expandable;
            this.api = item.Item as IApiType;
        }

        public bool Expandable { get; }

        public string GetPrefix()
        {
            string changeImg = item.DiffType == DiffType.Added ? "added" : item.DiffType == DiffType.Removed ? "removed" : "changed";
            string typeImg = GetTypeImg();
            string safeShortName = api.ShortName.Esc();
            if (Expandable)
            {
                string id = "id" + Guid.NewGuid().ToString("N");
                return string.Format("<li class=\"file\"><img class=\"type\" src=\"{0}.png\"/><img src=\"{1}.gif\"/<label for=\"{3}\">{2}</label> <input checked type=\"checkbox\" id=\"{3}\" /><ol>", changeImg, typeImg, safeShortName, id);
            }
            else
            {
                return
                    $"<li class=\"file\"><img class=\"type\" src=\"{changeImg}.png\"/><img src=\"{typeImg}.gif\"/><p>{safeShortName}</p>";
            }
        }

        private string GetTypeImg()
        {
            if (api is ApiClass) return "class";
            if (api is ApiInterface) return "interface";
            if (api is ApiStruct) return "struct";
            if (api is ApiEnum) return "enum";
            if (api is ApiDelegate) return "delegate";
            return string.Empty;
        }

        public string GetSuffix()
        {
            return Expandable ? "</ol></li>" : "</li>";
        }
    }

    public class ApiMemberWriter : IWriter
    {
        private readonly IDetail detail;
        private readonly DiffItem item;

        public ApiMemberWriter(DiffItem item)
        {
            this.item = item;
            this.detail = item.Item as IDetail;
        }

        public bool Expandable => false;

        public string GetPrefix()
        {
            string changeImg = item.DiffType == DiffType.Added ? "added" : item.DiffType == DiffType.Removed ? "removed" : "changed";
            string typeImg = GetMemberImg();
            string safeShortString = detail.ShortString.Esc();
            return string.Format("<li class=\"file\"><img class=\"type\" src=\"{1}.png\"/><img src=\"{2}.gif\"/><p>{0}</p>", safeShortString, changeImg, typeImg);
        }

        private string GetMemberImg()
        {
            if (detail is ApiConstructor) return "method";
            if (detail is ApiProperty) return "property";
            if (detail is ApiField) return "field";
            if (detail is ApiMethod) return "method";
            if (detail is ApiEvent) return "event";
            if (detail is ApiEnumValue) return "enum";
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
}