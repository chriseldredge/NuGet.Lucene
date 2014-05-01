using AspNet.WebApi.HtmlMicrodataFormatter;
using NuGet.Lucene.Web.SignalR.Hubs;

namespace NuGet.Lucene.Web.Controllers
{
    /// <summary>
    /// Provides documentation and semantic information about various
    /// resources and actions configured for use in this application.
    /// </summary>
    public class NuGetDocumentationController : DocumentationController
    {
        public NuGetWebApiRouteMapper NuGetWebApiRouteMapper { get; set; }

        /// <summary>
        /// Probably the document you are reading now.
        /// </summary>
        public override SimpleApiDocumentation GetApiDocumentation()
        {
            var docs = base.GetApiDocumentation();
            
            docs.Add("Packages", new SimpleApiDescription(Request, "OData", NuGetWebApiRouteMapper.ODataRoutePath)
                {
                    Documentation = DocumentationProvider.GetDocumentation(typeof(PackagesODataController))
                });
            
            docs.Add("Indexing", new SimpleApiDescription(Request, "Hub", NuGetWebApiRouteMapper.SignalrRoutePath)
                {
                    Documentation = DocumentationProvider.GetDocumentation(typeof(StatusHub))
                });


            return docs;
        }
    }

}
