using System;
using System.Reflection;
using Microsoft.AspNet.SignalR.Infrastructure;
using Newtonsoft.Json.Serialization;

namespace NuGet.Lucene.Web.SignalR
{
    /// <summary>
    /// Uses default contract resolver for types in the SignalR assembly
    /// and camel case for all other types.
    /// </summary>
    public class SelectiveCamelCaseContractResolver : IContractResolver
    {
        private readonly Assembly signalrAssembly;
        private readonly IContractResolver camelCaseContractResolver;
        private readonly IContractResolver defaultContractSerializer;

        public SelectiveCamelCaseContractResolver()
        {
            defaultContractSerializer = new DefaultContractResolver();
            camelCaseContractResolver = new CamelCasePropertyNamesContractResolver();
            signalrAssembly = typeof(Connection).Assembly;
        }

        public JsonContract ResolveContract(Type type)
        {
            if (type.Assembly.Equals(signalrAssembly))
                return defaultContractSerializer.ResolveContract(type);

            return camelCaseContractResolver.ResolveContract(type);
        }
    }
}