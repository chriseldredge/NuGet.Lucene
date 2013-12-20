This project contains a class library that provides a NuGet IPackageRepository
powered by Lucene.Net.Linq to provide very fast package listing, lookup, search
and aggregation functionality, and an Asp.NET Web Api project that exposes
a NuGet package feed with related functionality.

## Available on NuGet Gallery

To install the [NuGet.Lucene.Web package](http://nuget.org/packages/NuGet.Lucene.Web),
run the following command in the [Package Manager Console](http://docs.nuget.org/docs/start-here/using-the-package-manager-console)

    PM> Install-Package NuGet.Lucene.Web

If you do not need the web server components, you can install the [NuGet.Lucene package](http://nuget.org/packages/NuGet.Lucene),
run the following command in the [Package Manager Console](http://docs.nuget.org/docs/start-here/using-the-package-manager-console)

    PM> Install-Package NuGet.Lucene

## Getting Started

See [NuGet.Lucene's readme](source/NuGet.Lucene/readme.markdown) for information on embedding NuGet.Lucene in your project.

## Reference Project

The reference usage of these libraries is [Klondike](https://github.com/themotleyfool/Klondike), a fully integrated web
application for hosting a NuGet package feed.

## Building From Source

NuGet packages need to be restored before loading the solution in Visual Studio.

You can do this by running this command from the top level directory:

    msbuild /t:RestoreSolutionPackages
