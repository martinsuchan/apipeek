using System;
using System.Runtime.Serialization;

namespace ApiPeek.Core.Model;

[DataContract]
public class ApiAssembly
{
    [DataMember]
    public string Name { get; set; }
    [DataMember]
    public ApiClass[] Classes { get; set; }
    [DataMember]
    public ApiStruct[] Structs { get; set; }
    [DataMember]
    public ApiInterface[] Interfaces { get; set; }
    [DataMember]
    public ApiEnum[] Enums { get; set; }
    [DataMember]
    public ApiDelegate[] Delegates { get; set; }

    public void Init()
    {
        Classes ??= Array.Empty<ApiClass>();
        Structs ??= Array.Empty<ApiStruct>();
        Interfaces ??= Array.Empty<ApiInterface>();
        Enums ??= Array.Empty<ApiEnum>();
        Delegates ??= Array.Empty<ApiDelegate>();
    }
}