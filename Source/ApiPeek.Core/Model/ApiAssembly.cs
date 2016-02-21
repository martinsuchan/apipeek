using System.Runtime.Serialization;

namespace ApiPeek.Core.Model
{
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
            if (Classes == null) Classes = new ApiClass[0];
            if (Structs == null) Structs = new ApiStruct[0];
            if (Interfaces == null) Interfaces = new ApiInterface[0];
            if (Enums == null) Enums = new ApiEnum[0];
            if (Delegates == null) Delegates = new ApiDelegate[0];
        }
    }
}