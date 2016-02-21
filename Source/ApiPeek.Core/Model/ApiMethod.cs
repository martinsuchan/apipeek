using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using ApiPeek.Core.Extensions;

namespace ApiPeek.Core.Model
{
    [DataContract]
    public class ApiMethod : ApiBaseItem, IDetail
    {
        [DataMember]
        public bool IsStatic { get; set; }
        [DataMember]
        public string ReturnType { get; set; }
        [DataMember]
        public ApiMethodParameter[] Parameters { get; set; }

        [IgnoreDataMember]
        public override string SortName => ShortString;

        [IgnoreDataMember]
        public override string ShortString => string.Format("{1}({2}){0} : {3}", PrefixString, Name, ParamString, ReturnType);

        [IgnoreDataMember]
        private string PrefixString => IsStatic ? " static" : "";

        [IgnoreDataMember]
        private string ParamString
        {
            get { return Parameters.IsNullOrEmpty() ? "" : string.Join(", ", Parameters.Select(p => p.Detail())); }
        }

        public void Init()
        {
            if (Parameters == null) Parameters = new ApiMethodParameter[0];
        }

        public static IEqualityComparer<ApiMethod> DetailComparer =
            ProjectionEqualityComparer<ApiMethod>.Create(x => x.Detail);

        [IgnoreDataMember]
        public string Detail => InDetail();

        public string detail;
        public string InDetail(string indent = "")
        {
            if (detail == null)
            {
                StringBuilder sb = new StringBuilder();
                if (IsStatic) sb.Append("static ");
                sb.AppendFormat("public {0} {1}", ReturnType, Name);
                sb.Append("(");
                sb.Append(ParamString);
                sb.Append(");");
                detail = sb.ToString();
            }
            return indent + detail;
        }
    }
}