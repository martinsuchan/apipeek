using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using ApiPeek.Core.Extensions;

namespace ApiPeek.Core.Model;

[DataContract]
public class ApiConstructor : ApiBaseItem, IDetail
{
    [DataMember]
    public ApiMethodParameter[] Parameters { get; set; }

    [IgnoreDataMember]
    public override string SortName => ShortString;

    [IgnoreDataMember]
    public override string ShortString => $"{PrefixString}({ParamString})";

    [IgnoreDataMember]
    private string PrefixString => "ctor";

    [IgnoreDataMember]
    private string ParamString
    {
        get { return Parameters.IsNullOrEmpty() ? "" : string.Join(", ", Parameters.Select(p => p.Detail())); }
    }

    public static IEqualityComparer<ApiConstructor> DetailComparer =
        ProjectionEqualityComparer<ApiConstructor>.Create(x => x.Detail);

    [IgnoreDataMember]
    public string Detail => InDetail();

    private string detail;
    public string InDetail(string indent = "")
    {
        if (detail == null)
        {
            StringBuilder sb = new StringBuilder("ctor");
            sb.Append("(");
            if (!Parameters.IsNullOrEmpty())
                sb.Append(string.Join(", ", Parameters.Select(p => p.Detail())));
            sb.Append(");");
            detail = sb.ToString();
        }
        return indent + detail;
    }
}