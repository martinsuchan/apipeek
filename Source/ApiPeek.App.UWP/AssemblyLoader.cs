using System;
using System.Diagnostics;
using System.Reflection;

namespace ApiPeek.Service
{
    internal class AssemblyLoader
    {
        public virtual Assembly[] GetAssemblies()
        {
            Assembly[] assemblies =
            {
                GetAssembly("Windows.AI.MachineLearning.Preview.IInferencingOptionsPreview, Windows.AI, Version=255.255.255.255, Culture=neutral, PublicKeyToken=null, ContentType=WindowsRuntime"), // not available in 8.1 frameworks
                GetAssembly<Windows.ApplicationModel.Package>(),
                GetAssembly<Windows.Data.Json.JsonArray>(),
                GetAssembly<Windows.Devices.Geolocation.Geocoordinate>(),
                GetAssembly<Windows.Foundation.PropertyType>(),
                GetAssembly("Windows.Gaming.Input.Gamepad, Windows.Gaming, Version=255.255.255.255, Culture=neutral, PublicKeyToken=null, ContentType=WindowsRuntime"), // not available in 8.1 frameworks
                GetAssembly<Windows.Globalization.Language>(),
                GetAssembly<Windows.Graphics.Display.DisplayOrientations>(),
                GetAssembly<Windows.Management.Deployment.DeploymentProgress>(),
                GetAssembly<Windows.Media.AudioProcessing>(),
                GetAssembly<Windows.Networking.HostName>(),
                GetAssembly("Windows.Perception.PerceptionTimestamp, Windows.Perception, Version=255.255.255.255, Culture=neutral, PublicKeyToken=null, ContentType=WindowsRuntime"), // not available in 8.1 frameworks
                GetAssembly<Windows.Security.Cryptography.Core.CryptographicHash>(),
                GetAssembly("Windows.Services.Maps.MapAddress, Windows.Services, Version=255.255.255.255, Culture=neutral, PublicKeyToken=null, ContentType=WindowsRuntime"), // not available in 8.1 frameworks
                GetAssembly<Windows.Storage.StorageFile>(),
                GetAssembly<Windows.System.LauncherOptions>(),
                GetAssembly<Windows.UI.Colors>(),
                GetAssembly("Windows.UI.Xaml.PointHelper, Windows.UI.Xaml, Version=255.255.255.255, Culture=neutral, PublicKeyToken=null, ContentType=WindowsRuntime"), // not available in 8.1 frameworks
                GetAssembly<Windows.Web.Http.HttpClient>(),
            };
            return assemblies;
        }

        protected static Assembly GetAssembly<T>()
        {
            try
            {
                return typeof(T).GetTypeInfo().Assembly;
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                return null;
            }
        }

        protected static Assembly GetAssembly(string typeQualifiedName)
        {
            try
            {
                return Type.GetType(typeQualifiedName).GetTypeInfo().Assembly;
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                return null;
            }
        }

        protected static Assembly GetAssemblyByName(string assemblyName)
        {
            try
            {
                return Assembly.Load(new AssemblyName(assemblyName));
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                return null;
            }
        }
    }
}