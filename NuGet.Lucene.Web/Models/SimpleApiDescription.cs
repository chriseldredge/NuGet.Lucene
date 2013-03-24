using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Description;

namespace NuGet.Lucene.Web.Models
{
    public class SimpleApiDescription
    {
        public string Name { get; private set; }
        public string Href { get; private set; }
        public string Method { get; private set; }
        public IEnumerable<SimpleApiParameterDescriptor> Parameters { get; private set; }

        public SimpleApiDescription(HttpRequestMessage request, ApiDescription apiDescription)
        {
            var href = GetAbsoluteUri(request, apiDescription.Route.RouteTemplate);

            if (!string.IsNullOrEmpty(apiDescription.ActionDescriptor.ActionName) && href.Contains("{action}"))
            {
                href = href.Replace("{action}", apiDescription.ActionDescriptor.ActionName);
            }

            this.Href = href;

            this.Name = apiDescription.ActionDescriptor.ControllerDescriptor.ControllerName;

            if (apiDescription.ActionDescriptor.ActionName != null)
            {
                this.Name += "." + apiDescription.ActionDescriptor.ActionName;
            }

            this.Method = apiDescription.HttpMethod.Method;
            this.Parameters = apiDescription.ParameterDescriptions.Select(pd => new SimpleApiParameterDescriptor(pd.ParameterDescriptor, apiDescription.RelativePath)).ToList();
        }

        private static string GetAbsoluteUri(HttpRequestMessage request, string relativePath)
        {
            return new Uri(request.RequestUri,
                           request.GetConfiguration().VirtualPathRoot + relativePath)
                .GetComponents(UriComponents.AbsoluteUri, UriFormat.SafeUnescaped);
        }

        public SimpleApiDescription(HttpRequestMessage request, string name, string relativePath)
        {
            this.Href = GetAbsoluteUri(request, relativePath);
            this.Name = name;
            this.Method = "GET";
            this.Parameters = Enumerable.Empty<SimpleApiParameterDescriptor>();
        }
    }

    public class SimpleApiParameterDescriptor
    {
        public string Name { get; private set; }
        public string CallingConvention { get; private set; }
        public object DefaultValue { get; private set; }
        public bool IsOptional { get; private set; }

        public SimpleApiParameterDescriptor(HttpParameterDescriptor arg, string routePath)
        {
            this.Name = arg.ParameterName;
            this.IsOptional = arg.IsOptional;

            if (this.IsOptional)
            {
                this.DefaultValue = arg.DefaultValue;
            }

            var indexOfQueryString = routePath.IndexOf('?');

            if (arg.ParameterBinderAttribute is FromBodyAttribute)
            {
                this.CallingConvention = "body";
            }
            else
            {
                var indexOfParameter = routePath.IndexOf("{" + arg.ParameterName + "}", StringComparison.InvariantCultureIgnoreCase);

                if (indexOfQueryString > 0 && indexOfParameter > indexOfQueryString)
                {
                    this.CallingConvention = "query-string";
                }
                else if (indexOfParameter > 0)
                {
                    this.CallingConvention = "uri";
                }
                else
                {
                    this.CallingConvention = "unknown";
                }
            }
        }
    }
}