using System.Collections.Generic;
using System.Runtime.Serialization;
using ApiPeek.Core.Extensions;

namespace ApiPeek.Core.Model;

[DataContract]
public class ApiEnumValue : ApiBaseItem, IDetail
{
    [DataMember]
    public string Value { get; set; }

    [IgnoreDataMember]
    public override string ShortString => $"{Name} : {Value},";

    [IgnoreDataMember]
    public override string SortName => NumValue.ToString("D10");

    public static IEqualityComparer<ApiEnumValue> DetailComparer =
        ProjectionEqualityComparer<ApiEnumValue>.Create(x => x.Detail);

    [IgnoreDataMember]
    public long NumValue => long.Parse(Value);

    [IgnoreDataMember]
    public string Detail => InDetail();

    private string detail;
    public string InDetail(string indent = "")
    {
        detail ??= $"{Name} = {Value},";
        return indent + detail;
    }
}