using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Threading;

namespace NuGet.Lucene.Mapping
{
    public class CachingVersionConverter : TypeConverter
    {
        private static readonly ReaderWriterLockSlim locks = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
        private static readonly IDictionary<string, Version> cache = new Dictionary<string, Version>();

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            var stringValue = value as string;

            locks.EnterUpgradeableReadLock();

            try
            {
                Version version;
                if (cache.TryGetValue(stringValue, out version))
                {
                    return version;
                }

                locks.EnterWriteLock();
                try
                {
                    var versionString = value as string;
                    version = string.IsNullOrWhiteSpace(versionString) ? null : new Version(versionString);
                    cache[stringValue] = version;
                    return version;
                }
                finally
                {
                    locks.ExitWriteLock();
                }
            }
            finally
            {
                locks.ExitUpgradeableReadLock();
            }
        }

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            return ((Version) value).ToString();
        }

        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return sourceType == typeof (string);
        }

        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            return destinationType == typeof (string);
        }
    }
}