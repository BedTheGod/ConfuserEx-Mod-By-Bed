using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace Perplex.NF.Runtime
{
    internal static class DynamicMethods
    {
        private static Dictionary<string, MethodInfo> _methods;
        internal static MethodInfo GetCreatedMethodInfo(DynamicMethod method)
        {
            if (_methods == null)
                _methods = new Dictionary<string, MethodInfo>();
            if (_methods.ContainsKey(method.Name))
                return _methods[method.Name];
            return null;
        }

        internal static void SetMethodInfo(DynamicMethod method, MethodInfo methodInfo)
        {
            if (!_methods.ContainsKey(method.Name))
                _methods.Add(method.Name, methodInfo);
            else
                _methods[method.Name] = methodInfo;
        }

        internal static MethodInfo GetMethod(string name, Type ownerType, Type[] parameters)
        {
            var method = ownerType.GetMethod(name, parameters);
            if (method == null)
            {
                foreach (var m in ownerType.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic))
                {
                    if (m.Name== name && HasSameParamSig(parameters, m.GetParameters()))
                    {
                        method = m;
                        break;
                    }
                }
            }
            if (method.IsGenericMethod)
                return method.GetGenericMethodDefinition();

            return method;
        }

        internal static ConstructorInfo GetConstructor(Type ownerType, Type[] parameters)
        {
            ConstructorInfo constructor = ownerType.GetConstructor(parameters);
            if (constructor == null)
            {
                foreach (var c in ownerType.GetConstructors())
                {
                    if (HasSameParamSig(parameters, c.GetParameters()))
                    {
                        constructor = c;
                        break;
                    }
                }
            }
            return constructor;
        }

        private static bool HasSameParamSig(Type[] fParameters, ParameterInfo[] sParameters)
        {
            if (fParameters.Length != sParameters.Length) return false;

            for(int i = 0; i < fParameters.Length; i++)
            {
                if (fParameters[i] != sParameters[i].ParameterType)
                    return false;
            }
            return true;
        }
    }
}
