## NuGet.Lucene - Overview

This package provides a class library that implements NuGet.Core's IPackageRepository and related interfaces
to provide very fast package listing, lookup, search and aggregation functionality.

This package does not serve as a standalone package feed or package server; instead it must be embedded in
other projects.

## Getting Started

Configuration is primarily handled by NuGet.Lucene.LuceneRepositoryConfigurator. Create an instance of this
class and set the following properties:

### Required

* PackagePath (location where .nupkg files are stored)
* LuceneIndexPath (location where the lucene index data is stored)

### Optional

* GroupPackageFilesById (default: true; store packages grouped into subdirectories by package ID)
* EnablePackageFileWatcher (default: false; use a file system watcher to keep package and index in sync)
* PackageHashAlgorithm (default: SHA512)

Once you have set properties, call Initialize(). Then you can access the following properties:

* Repository (the IPackageRepository implementation)
* Provider (LuceneDataProvider used by Lucene.Net.Linq)
* LuceneDirectory (the Directory implementation in use)

## Cleaning Up

LuceneRepositoryConfigurator implements IDisposable. To ensure that the Lucene index is
properly flushed and closed, call Dispose on LuceneRepositoryConfigurator and it will
dispose related objects.
