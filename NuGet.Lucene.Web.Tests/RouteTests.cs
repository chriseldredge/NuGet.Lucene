namespace NuGet.Lucene.Web.Tests
{
    public abstract class RouteTests
    {
        public RouteResolveContstraint HasRouteFor(string url)
        {
            return new RouteResolveContstraint(url);
        }

        public RouteResolveContstraint HasRouteFor(string url, string method)
        {
            return new RouteResolveContstraint(url, method);
        }
    }
}