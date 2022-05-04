using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;
using ApiPeek.Core.Extensions;

namespace ApiPeek.Core.Model;

[DataContract]
public class ApiProperty : ApiBaseItem, IDetail
{
    [DataMember]
    public string Type { get; set; }
    [DataMember]
    public bool IsStatic { get; set; }
    [DataMember]
    public bool IsGet { get; set; }
    [DataMember]
    public bool IsSet { get; set; }

    [IgnoreDataMember]
    public override string SortName => ShortString;

    [IgnoreDataMember]
    public override string ShortString => string.Format("{1}{0} {{ {2}{3}}} : {4}",
        PrefixString, Name, GetString, SetString, Type);

    [IgnoreDataMember]
    private string PrefixString => IsStatic ? " static" : "";

    [IgnoreDataMember]
    private string GetString => IsGet ? "get; " : "";

    [IgnoreDataMember]
    private string SetString => IsSet ? "set; " : "";

    public static IEqualityComparer<ApiProperty> DetailComparer =
        ProjectionEqualityComparer<ApiProperty>.Create(x => x.Detail);

    [IgnoreDataMember]
    public string Detail => InDetail();

    private string detail;
    public string InDetail(string indent = "")
    {
        if (detail == null)
        {
            StringBuilder sb = new StringBuilder("public ");
            if (IsStatic) sb.Append("static ");
            sb.AppendFormat("{0} {1} {{ {2}{3}}}", Type, Name, GetString, SetString);
            detail = sb.ToString();
        }
        return indent + detail;
    }
}