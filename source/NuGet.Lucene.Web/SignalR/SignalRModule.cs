using Autofac;
using Autofac.Integration.SignalR;

namespace NuGet.Lucene.Web.SignalR
{
    public class SignalRModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterInstance(new SignalRMapper()).PropertiesAutowired();
            builder.RegisterHubs(typeof (SignalRModule).Assembly).PropertiesAutowired();
        }
    }
}