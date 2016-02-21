using System.Runtime.Serialization;

namespace ApiPeek.Core.Model
{
    [DataContract]
    public class ApiMethodParameter
    {
        [DataMember]
        public string Name { get; set; }
        [DataMember]
        public string Type { get; set; }
        [DataMember]
        public bool Out { get; set; }

        public string Detail()
        {
            return $"{Type} {Name}";
        }
    }
}