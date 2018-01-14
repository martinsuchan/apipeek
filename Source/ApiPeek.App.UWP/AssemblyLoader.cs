using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Windows.Web.Http;

namespace ApiPeek.Service
{
    internal class AssemblyLoader
    {
        public async Task<Assembly[]> GetAssembliesAsync()
        {
            AssemblyInfoModel assemblyModel = await GetOnlineAssemblies();
            if (assemblyModel == null) return new Assembly[0];

            Assembly[] assemblies = assemblyModel.Assemblies.Select(GetAssembly).ToArray();
            return assemblies;
        }

        protected static Assembly GetAssembly(AssemblyInfo ai)
        {
            try
            {
                string typeQualifiedName = $"{ai.Name}.{ai.Type}, {ai.Name}, Version=255.255.255.255, Culture=neutral, PublicKeyToken=null, ContentType=WindowsRuntime";
                return Type.GetType(typeQualifiedName).GetTypeInfo().Assembly;
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                return null;
            }
        }

        public async Task<AssemblyInfoModel> GetOnlineAssemblies()
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    Uri addr = new Uri("https://winconfig.azurewebsites.net/apipeek/config.json");

                    HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, addr);
                    HttpResponseMessage response = await client.SendRequestAsync(request);
                    if (!response.IsSuccessStatusCode) return null;

                    string result = await response.Content.ReadAsStringAsync();
                    AssemblyInfoModel model = JsonConvert.DeserializeObject<AssemblyInfoModel>(result);
                    return model.IsValid() ? model : null;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading online config.json: {ex}");
                return null;
            }
        }
    }

    public class AssemblyInfoModel
    {
        public AssemblyInfo[] Assemblies { get; set; }
        public int Version { get; set; }

        public bool IsValid()
        {
            return Assemblies != null && Assemblies.Length > 0
                && Assemblies.All(a => !string.IsNullOrWhiteSpace(a.Name) && !string.IsNullOrWhiteSpace(a.Type));
        }
    }

    public class AssemblyInfo
    {
        public string Name { get; set; }
        public string Type { get; set; }

        public AssemblyInfo(string name, string type)
        {
            Name = name;
            Type = type;
        }
    }
}