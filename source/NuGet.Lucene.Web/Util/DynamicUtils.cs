using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace NuGet.Lucene.Web.Util
{
    public static class DynamicUtils
    {
        public static T ShallowClone<T>(this T source) where T : new()
        {
            return source.ShallowClone(new T());
        }

        public static TDest ShallowClone<TSource, TDest>(this TSource source, TDest dest) where TDest : TSource
        {
            foreach (var kv in source.ToKeyValues(typeof(TSource)))
            {
                if (kv.Key.CanRead && kv.Key.CanWrite)
                {
                    kv.Key.SetValue(dest, kv.Value);
                }
            }

            return dest;
        }

        public static IEnumerable<KeyValuePair<PropertyInfo, object>> ToKeyValues(this object obj)
        {
            return obj.ToKeyValues(obj.GetType());
        }

        public static IEnumerable<KeyValuePair<PropertyInfo, object>> ToKeyValues(this object obj, Type type)
        {
            var props = type.GetProperties(BindingFlags.Instance | BindingFlags.Public);
            return props.Select(p => new KeyValuePair<PropertyInfo, object>(p, p.GetValue(obj)));
        }
    }
}