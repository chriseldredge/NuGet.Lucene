using System;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Runtime.Serialization;
using System.Web.Http;
using System.Web.Http.OData;
using System.Web.Http.OData.Builder;
using System.Web.Http.OData.Formatter;
using System.Web.Http.Validation.Providers;
using System.Web.Routing;
using System.Xml.Serialization;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using NuGet.Lucene.Web.Models;

namespace NuGet.Lucene.Web.Mvc4.Api
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            var modelBuilder = new ODataConventionModelBuilder();
            
            var packages = modelBuilder.EntitySet<ApiV2Package>("Packages");
            packages.HasReadLink(ctx => MapPackageUrl(ctx.EntityInstance));
            
            var package = packages.EntityType;

            package.HasKey(p => p.Id);
            package.HasKey(p => p.Version);
            config.Formatters.Clear();
            config.EnableOData(modelBuilder.GetEdmModel(), "api/v2");
            
            MapRoutes(RouteTable.Routes);
        }

        private static Uri MapPackageUrl(ApiV2Package package)
        {
            return new Uri("http://localhost/fake/path/to/" + package.Id + "/" + package.Version);
        }

        private static void MapRoutes(RouteCollection routes)
        {
            //routes.MapHttpRoute("v2 package feed", "api/v2/Packages", new { controller = "Packages", action = "Get" });
        }

        /*
        public static void IgnoreSerializableAttribute(JsonMediaTypeFormatter formatter)
        {
            var contractResolver = formatter.SerializerSettings.ContractResolver as DefaultContractResolver;

            if (contractResolver != null)
            {
                contractResolver.IgnoreSerializableAttribute = true;
            }

            formatter.SerializerSettings.ContractResolver = new DefaultContractResolver();
        }
         * */
    }
}
