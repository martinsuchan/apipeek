using System;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using ApiPeek.Core.Extensions;

namespace ApiPeek.Core.Model;

[DataContract]
public class ApiEnum : ApiBaseItem, IApiType
{
    [DataMember]
    public bool IsFlags { get; set; }
    [DataMember]
    public string BaseType { get; set; }
    [DataMember]
    public ApiEnumValue[] Values { get; set; }

    [IgnoreDataMember]
    public override string ShortString => string.Format("{1} {0} : {2}", PrefixString, NameSegments.Last(), BaseType);

    [IgnoreDataMember]
    private string PrefixString => IsFlags ? "[Flags] enum" : "enum";

    [IgnoreDataMember]
    public override ApiBaseItem[][] ApiItemsItems => new ApiBaseItem[][] { Values };

    public void Init()
    {
        Values ??= Array.Empty<ApiEnumValue>();
    }

    public string Detail(string indent = "")
    {
        StringBuilder sb = new StringBuilder(indent);
        sb.AppendLine(DetailStart);
        if (!Values.IsNullOrEmpty())
        {
            foreach (ApiEnumValue value in Values)
            {
                sb.AppendLine(value.InDetail(indent + "    "));
            }
        }
        sb.AppendLine(DetailEnd);
        return sb.ToString();
    }

    [IgnoreDataMember]
    public string DetailStart =>
        $"{(IsFlags ? "[Flags]\n" : "")}public enum {Name} {(BaseType != null ? $": {BaseType} " : "")}{{";

    [IgnoreDataMember]
    public string DetailEnd => "}";
}