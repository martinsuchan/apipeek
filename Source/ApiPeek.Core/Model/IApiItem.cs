using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using ApiPeek.Core.Extensions;

namespace ApiPeek.Core.Model
{
    [DataContract]
    [DebuggerDisplay("{Name}")]
    public abstract class ApiBaseItem
    {
        [DataMember]
        public string Name { get; set; }
        [IgnoreDataMember]
        public virtual string SortName => Name;

        [IgnoreDataMember]
        public string[] NameSegments => nameSegments ?? (nameSegments = Name.Split('.'));

        private string[] nameSegments;

        [IgnoreDataMember]
        public string ShortName => NameSegments.Last();

        [IgnoreDataMember]
        public virtual ApiBaseItem[][] ApiItemsItems => new ApiBaseItem[0][];

        [IgnoreDataMember]
        public virtual string ShortString => Name;

        public static IEqualityComparer<ApiBaseItem> SortNameComparer =
            ProjectionEqualityComparer<ApiBaseItem>.Create(x => x.SortName);
    }

    public class ApiNamespace : ApiBaseItem
    {
        public ApiBaseItem[] ApiItems { get; set; }
        public ApiNamespace[] Namespaces { get; set; }

        public ApiNamespace(string name)
        {
            Name = name;
            ApiItems = new ApiBaseItem[0];
            Namespaces = new ApiNamespace[0];
        }

        public override ApiBaseItem[][] ApiItemsItems => new[] { Namespaces, ApiItems };
    }

    public class ApiRoot : ApiBaseItem {}

    public class DiffItem
    {
        public ApiBaseItem Item { get; set; }
        public DiffType DiffType { get; set; }
        public List<DiffItem> Children { get; set; }

        public bool Any()
        {
            return DiffType == DiffType.Added
                || DiffType == DiffType.Removed
                || Children.Any();
        }

        public string TypePrefix
        {
            get
            {
                switch (DiffType)
                {
                    case DiffType.Added: return "+";
                    case DiffType.Removed: return "-";
                }
                return string.Empty;
            }
        }

        public DiffItem()
        {
            Children = new List<DiffItem>();
        }
    }

    public enum DiffType
    {
        Added,
        Removed,
        Changed
    }
}