using System;
using System.Reflection;

namespace NuGet.Lucene.Web
{
    internal class SimpleParameterInfo<T> : ParameterInfo
    {
        private readonly string name;
        private readonly T defaultValue;
        private readonly bool hasDefault;
        private readonly ParameterAttributes attributes;

        public SimpleParameterInfo(string name)
        {
            this.name = name;
            this.defaultValue = default(T);
            this.hasDefault = false;
            this.attributes = ParameterAttributes.None;
        }

        public SimpleParameterInfo(string name, T defaultValue)
        {
            this.name = name;
            this.defaultValue = defaultValue;
            this.hasDefault = true;
            this.attributes = ParameterAttributes.Optional;
        }

        public override ParameterAttributes Attributes
        {
            get { return attributes; }
        }

        public override bool HasDefaultValue { get { return hasDefault; } }

        public override object DefaultValue
        {
            get
            {
                return defaultValue;
            }
        }

        public override Type ParameterType { get { return typeof(T); } }

        public override string Name { get { return name; } }

        public override Object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            return (object[])Array.CreateInstance(attributeType, 0);
        }

        
    }
}