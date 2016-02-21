using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using ApiPeek.Core.Extensions;

namespace ApiPeek.Core.Model
{
    [DataContract]
    public class ApiInterface : ApiBaseItem, IApiType
    {
        [DataMember]
        public string[] Interfaces { get; set; }
        [DataMember]
        public ApiProperty[] Properties { get; set; }
        [DataMember]
        public ApiMethod[] Methods { get; set; }
        [DataMember]
        public ApiEvent[] Events { get; set; }

        [IgnoreDataMember]
        public override string ShortString => $"{NameSegments.Last()} interface";

        [IgnoreDataMember]
        public override ApiBaseItem[][] ApiItemsItems
        {
            get
            {
                ApiBaseItem[] uniList = new ApiBaseItem[][] { Properties, Methods, Events }
                    .Where(a => !a.IsNullOrEmpty()).SelectMany(a => a).ToArray();
                return new[] { uniList };
            }
        }

        public void Init()
        {
            if (Interfaces == null) Interfaces = new string[0];
            if (Properties == null) Properties = new ApiProperty[0];
            if (Methods == null) Methods = new ApiMethod[0];
            if (Events == null) Events = new ApiEvent[0];
        }

        public string Detail(string indent = "")
        {
            StringBuilder sb = new StringBuilder(indent);
            sb.AppendLine(DetailStart);
            if (!Properties.IsNullOrEmpty())
            {
                foreach (ApiProperty property in Properties)
                {
                    sb.AppendLine(property.InDetail(indent + "    "));
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
                return $"public interface {Name}{parents} {{";
            }
        }

        [IgnoreDataMember]
        public string DetailEnd => "}";
    }
}