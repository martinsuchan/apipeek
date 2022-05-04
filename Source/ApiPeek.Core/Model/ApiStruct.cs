using System;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using ApiPeek.Core.Extensions;

namespace ApiPeek.Core.Model;

[DataContract]
public class ApiStruct : ApiBaseItem, IApiType
{
    [DataMember]
    public string[] Interfaces { get; set; }
    [DataMember]
    public ApiConstructor[] Constructors { get; set; }
    [DataMember]
    public ApiProperty[] Properties { get; set; }
    [DataMember]
    public ApiField[] Fields { get; set; }
    [DataMember]
    public ApiMethod[] Methods { get; set; }
    [DataMember]
    public ApiEvent[] Events { get; set; }

    [IgnoreDataMember]
    public override string ShortString => $"{NameSegments.Last()} struct";

    [IgnoreDataMember]
    public override ApiBaseItem[][] ApiItemsItems
    {
        get
        {
            ApiBaseItem[] uniList = new ApiBaseItem[][] { Constructors, Properties, Fields, Methods, Events }
                .Where(a => !a.IsNullOrEmpty()).SelectMany(a => a).ToArray();
            return new[] { uniList };
        }
    }

    public void Init()
    {
        Interfaces ??= Array.Empty<string>();
        Constructors ??= Array.Empty<ApiConstructor>();
        Properties ??= Array.Empty<ApiProperty>();
        Fields ??= Array.Empty<ApiField>();
        Methods ??= Array.Empty<ApiMethod>();
        Events ??= Array.Empty<ApiEvent>();
    }

    public string Detail(string indent = "")
    {
        StringBuilder sb = new StringBuilder(indent);
        sb.AppendLine(DetailStart);
        if (!Constructors.IsNullOrEmpty())
        {
            foreach (ApiConstructor ctor in Constructors)
            {
                sb.AppendLine(ctor.InDetail(indent + "    "));
            }
        }
        if (!Properties.IsNullOrEmpty())
        {
            foreach (ApiProperty property in Properties)
            {
                sb.AppendLine(property.InDetail(indent + "    "));
            }
        }
        if (!Fields.IsNullOrEmpty())
        {
            foreach (ApiField field in Fields)
            {
                sb.AppendLine(field.InDetail(indent + "    "));
            }
        }
        if (!Methods.IsNullOrEmpty())
        {
            foreach (ApiMethod method in Methods)
            {
                sb.AppendLine(method.InDetail(indent + "    "));
            }
        }
        if (!Events.IsNullOrEmpty())
        {
            foreach (ApiEvent evnt in Events)
            {
                sb.AppendLine(evnt.InDetail(indent + "    "));
            }
        }
        sb.AppendLine(DetailEnd);
        return sb.ToString();
    }

    [IgnoreDataMember]
    public string DetailStart
    {
        get
        {
            string parents = Interfaces.Length > 0 ? $"\n    : {string.Join(", ", Interfaces)}" : "";
            return $"public struct {Name}{parents} {{";
        }
    }

    [IgnoreDataMember]
    public string DetailEnd => "}";
}