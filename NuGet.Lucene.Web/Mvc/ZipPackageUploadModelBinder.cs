using System;
using System.Web.Mvc;

namespace NuGet.Lucene.Web.Mvc
{
    public class ZipPackageUploadModelBinder : DefaultModelBinder
    {
        public override object BindModel(ControllerContext controllerContext, ModelBindingContext bindingContext)
        {
            var request = controllerContext.HttpContext.Request;
            var stream = request.Files.Count > 0 ? request.Files[0].InputStream : request.InputStream;

            return new ZipPackage(stream);
        }
    }
}