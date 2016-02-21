using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using ApiPeek.Core.Extensions;

namespace ApiPeek.Core.Model
{
    [DataContract]
    public class ApiDelegate : ApiBaseItem, IDetail, IApiType
    {
        [DataMember]
        public string ReturnType { get; set; }
        [DataMember]
        public ApiMethodParameter[] Parameters { get; set; }

        [IgnoreDataMember]
        public override string SortName => ShortString;

        [IgnoreDataMember]
        public override string ShortString => $"{NameSegments.Last()} delegate";

        public void Init()
        {
            if (Parameters == null) Parameters = new ApiMethodParameter[0];
        }

        public static IEqualityComparer<ApiDelegate> DetailComparer =
            ProjectionEqualityComparer<ApiDelegate>.Create(x => x.Detail);

        [IgnoreDataMember]
        public string Detail => InDetail();

        public string detail;
        public string InDetail(string indent = "")
        {
            if (detail == null)
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendFormat("public delegate {0} {1}", ReturnType, Name);
                sb.Append("(");
                if (!Parameters.IsNullOrEmpty())
                    sb.Append(string.Join(", ", Parameters.Select(p => p.Detail())));
                sb.Append(");");
                detail = sb.ToString();
            }
            return indent + detail;
        }
    }
}