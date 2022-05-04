using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;
using ApiPeek.Core.Extensions;

namespace ApiPeek.Core.Model;

[DataContract]
public class ApiField : ApiBaseItem, IDetail
{
    [DataMember]
    public string Type { get; set; }
    [DataMember]
    public bool IsStatic { get; set; }

    [IgnoreDataMember]
    public override string SortName => ShortString;

    [IgnoreDataMember]
    public override string ShortString => string.Format("{1}{0} : {2}", PrefixString, Name, Type);

    [IgnoreDataMember]
    private string PrefixString => IsStatic ? " static" : "";

    public static IEqualityComparer<ApiField> DetailComparer =
        ProjectionEqualityComparer<ApiField>.Create(x => x.Detail);

    [IgnoreDataMember]
    public string Detail => InDetail();

    private string detail;
    public string InDetail(string indent = "")
    {
        if (detail == null)
        {
            StringBuilder sb = new StringBuilder("public ");
            if (IsStatic) sb.Append("static ");
            sb.AppendFormat("{0} {1};", Type, Name);
            detail = sb.ToString();
        }
        return indent + detail;
    }
}