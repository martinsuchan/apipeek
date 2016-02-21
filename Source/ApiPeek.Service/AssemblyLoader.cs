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
                GetAssembly<Windows.ApplicationModel.Package>(),
                GetAssembly<Windows.Data.Json.JsonArray>(),
                GetAssembly<Windows.Devices.Geolocation.Geocoordinate>(),
                GetAssembly<Windows.Foundation.PropertyType>(),
                //GetAssembly<Windows.Gaming.Input.Gamepad>(), Windows.Gaming not available in 8.1 frameworks
                GetAssembly("Windows.Gaming.Input.Gamepad, Windows.Gaming, Version=255.255.255.255, Culture=neutral, PublicKeyToken=null, ContentType=WindowsRuntime"),
                GetAssembly<Windows.Globalization.Language>(),
                GetAssembly<Windows.Graphics.Display.DisplayOrientations>(),
                GetAssembly<Windows.Management.Deployment.DeploymentProgress>(),
                GetAssembly<Windows.Media.AudioProcessing>(),
                GetAssembly<Windows.Networking.HostName>(),
                //GetAssembly<Windows.Perception.PerceptionTimestamp>(), Windows.Perception not available in 8.1 frameworks
                GetAssembly("Windows.Perception.PerceptionTimestamp, Windows.Perception, Version=255.255.255.255, Culture=neutral, PublicKeyToken=null, ContentType=WindowsRuntime"),
                GetAssembly<Windows.Security.Cryptography.Core.CryptographicHash>(),
                //GetAssembly<Windows.Services.Maps.MapAddress>(), Windows.Services not available in 8.1 frameworks
                GetAssembly("Windows.Services.Maps.MapAddress, Windows.Services, Version=255.255.255.255, Culture=neutral, PublicKeyToken=null, ContentType=WindowsRuntime"),
                GetAssembly<Windows.Storage.StorageFile>(),
                GetAssembly<Windows.System.LauncherOptions>(),
                GetAssembly<Windows.UI.Colors>(),
                //GetAssembly<Windows.UI.Xaml.PointHelper>(), Windows.UI.XAML not available in SL8.1
                GetAssembly("Windows.UI.Xaml.PointHelper, Windows.UI.Xaml, Version=255.255.255.255, Culture=neutral, PublicKeyToken=null, ContentType=WindowsRuntime"),
                GetAssembly<Windows.Web.Http.HttpClient>(),
#if WINDOWS_PHONE
                GetAssembly<System.Windows.Application>(),
                GetAssembly<Microsoft.Phone.BackgroundAgent>(),
#endif
                /*GetAssembly("Microsoft.Band.BandException, Microsoft.Band, Version=1.3.10219.1, Culture=neutral, PublicKeyToken=608d7da3159f502b"),
                GetAssembly("Microsoft.Band.BandIconExtensions, Microsoft.Band.Store, Version=1.3.10219.1, Culture=neutral, PublicKeyToken=608d7da3159f502b"),
                GetAssembly("Microsoft.Band.BandClientManager, Microsoft.Band.Phone, Version=1.3.10219.1, Culture=neutral, PublicKeyToken=608d7da3159f502b"),*/
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