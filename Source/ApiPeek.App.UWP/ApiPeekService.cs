using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Windows.Foundation;
using Windows.Storage;

namespace ApiPeek.Service
{
    public sealed class ApiPeekService
    {
        public static IAsyncOperation<StorageFile> PeekAndLog()
        {
            return PeekAndLogInternal().AsAsyncOperation();
        }

        internal static async Task<StorageFile> PeekAndLogInternal()
        {
            try
            {
                AssemblyLoader loader = new AssemblyLoader();
                Assembly[] assemblies = await loader.GetAssembliesAsync();
                assemblies = assemblies.Where(a => a != null).Distinct().ToArray();

                string fileName = $"peek-{PlatformService.SystemVersion}-{DateTime.Now:yyyyMMddHHmmss}";

                // save it to json -> ZIP file
                StorageFile file = await ApplicationData.Current.LocalFolder
                    .CreateFileAsync("log\\" + fileName, CreationCollisionOption.ReplaceExisting);
                using (Stream fileStream = await file.OpenStreamForWriteAsync())
                {
                    using (ZipArchive zip = new ZipArchive(fileStream, ZipArchiveMode.Create, true))
                    {
                        foreach (Assembly assembly in assemblies)
                        {
                            string asmName = GetAssemblyName(assembly);

                            ZipArchiveEntry telemetryFile = zip.CreateEntry(asmName + ".json", CompressionLevel.Optimal);
                            using (StreamWriter writer = new StreamWriter(telemetryFile.Open()))
                            {
                                JsonTextWriter jsonWriter = new JsonTextWriter(writer) { Formatting = Formatting.Indented };

                                Peek(assembly, jsonWriter);
                                jsonWriter.Flush();
                                jsonWriter.Close();
                            }
                        }
                    }
                }
                return file;
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                return null;
            }
        }

        private static string GetAssemblyName(Assembly assembly)
        {
            return assembly.FullName.Split(',')[0];
        }

        private static IEnumerable<TypeInfo> GetDefinedTypes(Assembly assembly)
        {
            try
            {
                return assembly.DefinedTypes;
            }
            catch (ReflectionTypeLoadException e)
            {
                Debug.WriteLine(e);
                return e.Types.Where(t => t != null).Select(t => t.GetTypeInfo()).Where(ti => ti != null);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                return Array.Empty<TypeInfo>();
            }
        }

        private static Type GetPropertyType(PropertyInfo prop)
        {
            try
            {
                return prop.PropertyType;
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                return null;
            }
        }

        #region Api gather

        internal static void Peek(Assembly assembly, JsonWriter jw)
        {
            try
            {
                jw.WriteStartObject();

                WriteString("Name", assembly.FullName, jw);
                WriteTypes("Enums", t => t.IsEnum, GetEnum, assembly, jw);
                WriteTypes("Interfaces", t => t.IsInterface, GetInterface, assembly, jw);
                WriteTypes("Structs", t => t.IsValueType && !t.IsEnum, GetStruct, assembly, jw);
                WriteTypes("Classes", IsClass, GetClass, assembly, jw);
                WriteTypes("Delegates", IsDelegate, GetDelegate, assembly, jw);

                jw.WriteEndObject();
            }
            catch (Exception e)
            {
                Debug.WriteLine("Error in Peek: {0}", e.Message);
            }
        }

        private static void WriteTypes(string name, Func<TypeInfo, bool> test, Action<TypeInfo, JsonWriter> action,
            Assembly assembly, JsonWriter jw)
        {
            //TypeInfo[] types = assembly.DefinedTypes.Where(t => t.IsPublic && test(t)).ToArray();
            TypeInfo[] types = GetDefinedTypes(assembly).Where(t => t.IsPublic && test(t)).ToArray();
            if (types.Length == 0) return;

            jw.WritePropertyName(name);
            jw.WriteStartArray();
            foreach (TypeInfo type in types)
            {
                action(type, jw);
            }
            jw.WriteEndArray();
        }

        private static void GetEnum(TypeInfo enm, JsonWriter jw)
        {
            jw.WriteStartObject();

            WriteString("Name", enm.AsType().GetFullTypeName(), jw);
            WriteBool("IsFlags", enm.GetCustomAttributes<FlagsAttribute>().Any(), jw);
            Type basetype = Enum.GetUnderlyingType(enm.AsType());
            WriteString("BaseType", basetype.Name, jw);
            GetEnumValues(enm, basetype, jw);

            jw.WriteEndObject();
        }

        private static void GetInterface(TypeInfo iface, JsonWriter jw)
        {
            jw.WriteStartObject();

            WriteString("Name", iface.AsType().GetFullTypeName(), jw);
            GetInterfaces(iface, jw);
            GetProperties(iface, jw);
            GetMethods(iface, jw);
            GetEvents(iface, jw);

            jw.WriteEndObject();
        }

        private static void GetStruct(TypeInfo strct, JsonWriter jw)
        {
            jw.WriteStartObject();

            WriteString("Name", strct.AsType().GetFullTypeName(), jw);
            GetInterfaces(strct, jw);
            GetConstructors(strct, jw);
            GetProperties(strct, jw);
            GetFields(strct, jw);
            GetMethods(strct, jw);
            GetEvents(strct, jw);

            jw.WriteEndObject();
        }

        private static readonly Type objType = typeof(object);

        private static void GetClass(TypeInfo clss, JsonWriter jw)
        {
            jw.WriteStartObject();

            string name = clss.AsType().GetFullTypeName();
            WriteString("Name", name, jw);
            WriteBool("IsStatic", clss.IsAbstract && clss.IsSealed, jw);
            WriteBool("IsSealed", !clss.IsAbstract && clss.IsSealed, jw);
            WriteBool("IsAbstract", clss.IsAbstract && !clss.IsSealed, jw);
            if (clss.BaseType != objType && clss.BaseType.GetTypeInfo().IsPublic) WriteString("BaseType", clss.BaseType.GetTypeName(), jw);
            GetInterfaces(clss, jw);
            GetConstructors(clss, jw);
            GetProperties(clss, jw);
            GetFields(clss, jw);
            GetMethods(clss, jw);
            GetEvents(clss, jw);

            jw.WriteEndObject();
        }

        private static void GetDelegate(TypeInfo del, JsonWriter jw)
        {
            jw.WriteStartObject();

            string name = del.AsType().GetFullTypeName();
            WriteString("Name", name, jw);
            MethodInfo invoke = del.DeclaredMethods.FirstOrDefault(m => m.Name == "Invoke");
            if (invoke == null)
            {
                Debugger.Break();
                return;
            }
            WriteString("ReturnType", invoke.ReturnType.GetTypeName(), jw);
            GetMethodParameters(invoke, jw);

            jw.WriteEndObject();
        }

        private static void GetEnumValues(TypeInfo enm, Type baseType, JsonWriter jw)
        {
            FieldInfo[] fields = enm.DeclaredFields.Where(InterestingName).ToArray();
            if (fields.Length == 0) return;

            jw.WritePropertyName("Values");
            jw.WriteStartArray();
            foreach (FieldInfo field in fields)
            {
                GetEnumValue(field, baseType, jw);
            }
            jw.WriteEndArray();
        }

        private static void GetEnumValue(FieldInfo par, Type baseType, JsonWriter jw)
        {
            jw.WriteStartObject();

            WriteString("Name", par.Name, jw);
            object fieldValue = Convert.ChangeType(par.GetValue(null), baseType);
            WriteString("Value", fieldValue.ToString(), jw);

            jw.WriteEndObject();
        }

        private static void GetInterfaces(TypeInfo type, JsonWriter jw)
        {
            Type[] interfaces = type.ImplementedInterfaces.Where(i => i.GetTypeInfo().IsPublic).ToArray();
            if (interfaces.Length == 0) return;

            jw.WritePropertyName("Interfaces");
            jw.WriteStartArray();
            foreach (Type ifaceType in interfaces)
            {
                jw.WriteValue(ifaceType.GetTypeName());
            }
            jw.WriteEndArray();
        }

        private static void GetConstructors(TypeInfo type, JsonWriter jw)
        {
            ConstructorInfo[] constructors = type.DeclaredConstructors.Where(InterestingName).ToArray();
            if (constructors.Length == 0) return;
            if (constructors.Length == 1)
            {
                // do not log if there is only the defualt constructor
                ParameterInfo[] parameters = constructors[0].GetParameters();
                if (parameters.IsNullOrEmpty()) return;
            }

            jw.WritePropertyName("Constructors");
            jw.WriteStartArray();
            foreach (ConstructorInfo ctor in constructors)
            {
                GetConstructor(ctor, jw);
            }
            jw.WriteEndArray();
        }

        private static void GetConstructor(ConstructorInfo ctor, JsonWriter jw)
        {
            jw.WriteStartObject();

            GetMethodParameters(ctor, jw);

            jw.WriteEndObject();
        }

        private static void GetProperties(TypeInfo type, JsonWriter jw)
        {
            //PropertyInfo[] properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            //PropertyInfo[] sproperties = type.GetProperties(BindingFlags.Public | BindingFlags.Static);
            PropertyInfo[] properties = type.DeclaredProperties.Where(InterestingName).ToArray();
            if (properties.Length == 0) return;

            jw.WritePropertyName("Properties");
            jw.WriteStartArray();
            foreach (PropertyInfo prop in properties)
            {
                GetProperty(prop, PropertyIsStatic(prop), jw);
            }
            jw.WriteEndArray();
        }

        private static void GetProperty(PropertyInfo prop, bool isStatic, JsonWriter jw)
        {
            jw.WriteStartObject();

            WriteString("Name", prop.Name, jw);
            WriteString("Type", GetPropertyType(prop).GetTypeName(), jw);
            WriteBool("IsStatic", isStatic, jw);
            WriteBool("IsGet", prop.CanRead, jw);
            WriteBool("IsSet", prop.CanWrite, jw);

            jw.WriteEndObject();
        }

        private static void GetFields(TypeInfo type, JsonWriter jw)
        {
            //FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance).Where(InterestingName).ToArray();
            //FieldInfo[] sfields = type.GetFields(BindingFlags.Public | BindingFlags.Static).Where(InterestingName).ToArray();
            FieldInfo[] fields = type.DeclaredFields.Where(InterestingName).ToArray();
            if (fields.Length == 0) return;

            jw.WritePropertyName("Fields");
            jw.WriteStartArray();
            foreach (FieldInfo field in fields)
            {
                GetField(field, field.IsStatic, jw);
            }
            jw.WriteEndArray();
        }

        private static void GetField(FieldInfo field, bool isStatic, JsonWriter jw)
        {
            jw.WriteStartObject();

            WriteString("Name", field.Name, jw);
            WriteString("Type", field.FieldType.GetTypeName(), jw);
            WriteBool("IsStatic", isStatic, jw);

            jw.WriteEndObject();
        }

        private static void GetMethods(TypeInfo type, JsonWriter jw)
        {
            //MethodInfo[] methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance).Where(InterestingName).ToArray();
            //MethodInfo[] smethods = type.GetMethods(BindingFlags.Public | BindingFlags.Static).Where(InterestingName).ToArray();
            MethodInfo[] methods = type.DeclaredMethods.Where(InterestingName).ToArray();
            if (methods.Length == 0) return;

            jw.WritePropertyName("Methods");
            jw.WriteStartArray();
            foreach (MethodInfo method in methods)
            {
                GetMethod(method, method.IsStatic, jw);
            }
            jw.WriteEndArray();
        }

        private static void GetMethod(MethodInfo meth, bool isStatic, JsonWriter jw)
        {
            jw.WriteStartObject();

            WriteString("Name", meth.Name, jw);
            WriteBool("IsStatic", isStatic, jw);
            WriteString("ReturnType", meth.ReturnType.GetTypeName(), jw);
            GetMethodParameters(meth, jw);

            jw.WriteEndObject();
        }

        private static void GetMethodParameters(MethodBase method, JsonWriter jw)
        {
            ParameterInfo[] parameters = method.GetParameters();
            if (parameters.Length == 0) return;

            jw.WritePropertyName("Parameters");
            jw.WriteStartArray();
            foreach (ParameterInfo par in parameters)
            {
                GetMethodParameter(par, jw);
            }
            jw.WriteEndArray();
        }

        private static void GetMethodParameter(ParameterInfo par, JsonWriter jw)
        {
            jw.WriteStartObject();

            WriteString("Name", par.Name, jw);
            WriteString("Type", par.ParameterType.GetTypeName(), jw);
            WriteBool("Out", par.IsOut, jw);

            jw.WriteEndObject();
        }

        private static void GetEvents(TypeInfo type, JsonWriter jw)
        {
            //EventInfo[] events = type.GetEvents(BindingFlags.Public | BindingFlags.Instance).ToArray();
            //EventInfo[] sevents = type.GetEvents(BindingFlags.Public | BindingFlags.Static).ToArray();
            EventInfo[] events = type.DeclaredEvents.ToArray();
            if (events.Length == 0) return;

            jw.WritePropertyName("Events");
            jw.WriteStartArray();
            foreach (EventInfo evnt in events)
            {
                GetEvent(evnt, EventIsStatic(evnt), jw);
            }
            jw.WriteEndArray();
        }

        private static void GetEvent(EventInfo evnt, bool isStatic, JsonWriter jw)
        {
            jw.WriteStartObject();

            WriteString("Name", evnt.Name, jw);
            WriteString("Type", evnt.EventHandlerType.GetTypeName(), jw);
            WriteBool("IsStatic", isStatic, jw);

            jw.WriteEndObject();
        }

        private static void WriteString(string name, string stringValue, JsonWriter jw)
        {
            if (string.IsNullOrWhiteSpace(stringValue)) return;
            jw.WritePropertyName(name);
            jw.WriteValue(stringValue);
        }

        private static void WriteBool(string name, bool boolValue, JsonWriter jw)
        {
            if (!boolValue) return;
            jw.WritePropertyName(name);
            jw.WriteValue(true);
        }

        private static readonly TypeInfo delegateTI = typeof(MulticastDelegate).GetTypeInfo();

        private static bool IsDelegate(TypeInfo ti)
        {
            return delegateTI.IsAssignableFrom(ti);
        }

        private static bool IsClass(TypeInfo ti)
        {
            return ti.IsClass && !delegateTI.IsAssignableFrom(ti);
        }

        private static bool PropertyIsStatic(PropertyInfo p)
        {
            return (p.GetMethod != null && p.GetMethod.IsStatic) || (p.SetMethod != null && p.SetMethod.IsStatic);
        }

        private static bool EventIsStatic(EventInfo e)
        {
            return (e.AddMethod != null && e.AddMethod.IsStatic) || (e.RemoveMethod != null && e.RemoveMethod.IsStatic);
        }

        private static readonly string[] ignoredMethods = { "GetHashCode", "ToString", "Equals", "GetType", "CompareTo", "HasFlag", "GetTypeCode" };
        private static bool InterestingName(MethodInfo mi)
        {
            return mi.IsPublic && !mi.IsSpecialName && !ignoredMethods.Contains(mi.Name);
        }

        private static readonly string[] ignoredProperties = Array.Empty<string>();
        private static bool InterestingName(PropertyInfo pi)
        {
            return !pi.IsSpecialName && !ignoredProperties.Contains(pi.Name);
        }

        private static readonly string[] ignoredFields = { "value__", };
        private static bool InterestingName(FieldInfo fi)
        {
            return fi.IsPublic && !fi.IsSpecialName && !ignoredFields.Contains(fi.Name);
        }

        private static readonly string[] ignoredConstructors = { };
        private static bool InterestingName(ConstructorInfo fi)
        {
            return fi.IsPublic && !ignoredConstructors.Contains(fi.Name);
        }

        #endregion
    }

    internal static class StringExt
    {
        public static bool IsNullOrEmpty<TSource>(this ICollection<TSource> collection)
        {
            return collection == null || !collection.Any();
        }

        public static string GetTypeName(this Type t)
        {
            if (t == null) return "unknown";
            TypeInfo ti = t.GetTypeInfo();
            if (!ti.IsGenericType) return t.Name;

            string genTypeName = t.GetGenericTypeDefinition().Name;
            if (genTypeName.Contains("`")) genTypeName = genTypeName.Substring(0, genTypeName.IndexOf('`'));
            string genArgs = string.Join(",", ti.GenericTypeParameters.Select(GetTypeName));
            if (genArgs == "") genArgs = string.Join(",", ti.GenericTypeArguments.Select(GetTypeName));
            if (genArgs == "") Debugger.Break();
            return genTypeName + "<" + genArgs + ">";
        }

        public static string GetFullTypeName(this Type t)
        {
            if (t == null) return "unknown";
            TypeInfo ti = t.GetTypeInfo();
            if (!ti.IsGenericType) return t.FullName;

            string genTypeName = t.GetGenericTypeDefinition().FullName;
            if (genTypeName.Contains("`")) genTypeName = genTypeName.Substring(0, genTypeName.IndexOf('`'));
            string genArgs = string.Join(",", ti.GenericTypeParameters.Select(GetTypeName));
            if (genArgs == "") genArgs = string.Join(",", ti.GenericTypeArguments.Select(GetTypeName));
            if (genArgs == "") Debugger.Break();
            return genTypeName + "<" + genArgs + ">";
        }
    }

    public static class PlatformService
    {
        public static Version SystemVersion { get; }

        private const string AiTypeName = "Windows.System.Profile.AnalyticsInfo, Windows.System, Version=255.255.255.255, Culture=neutral, PublicKeyToken=null, ContentType=WindowsRuntime";
        private const string AviTypeName = "Windows.System.Profile.AnalyticsVersionInfo, Windows.System, Version=255.255.255.255, Culture=neutral, PublicKeyToken=null, ContentType=WindowsRuntime";

        static PlatformService()
        {
            bool osSet = false;
            try
            {
                Type aiType = Type.GetType(AiTypeName);
                // if the type exists, the app is running on Windows 10
                if (aiType != null)
                {
                    Type aviType = Type.GetType(AviTypeName);
                    var avi = aiType.GetRuntimeProperty("VersionInfo").GetValue(null);
                    string sv = aviType.GetRuntimeProperty("DeviceFamilyVersion").GetValue(avi) as string;
                    ulong v = ulong.Parse(sv);
                    ulong v1 = (v & 0xFFFF000000000000L) >> 48;
                    ulong v2 = (v & 0x0000FFFF00000000L) >> 32;
                    ulong v3 = (v & 0x00000000FFFF0000L) >> 16;
                    ulong v4 = (v & 0x000000000000FFFFL);
                    SystemVersion = new Version((int)v1, (int)v2, (int)v3, (int)v4);
                    osSet = true;
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
            if (osSet) return;
            SystemVersion = new Version(8, 1);
        }
    }
}