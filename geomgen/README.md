GeometryGeneration has a C# solution with a set of projects that perform geometry generation, stitching and visualization (see an example in GeometryGeneration/Program.cs).
Bulk of geometry generation logic is in PostProcessing project including morphology operations, thinning, graph construction, connections finder and graph optimization (annealing). 

Code can also be used for various additional purposes for example
- calculating metrics in PostProcessing\Metrics
- satellite image downloading in ImageUtils/ImageDownload/DownloadImageUtils (for Bing satellite imagery of different resolutions, sizes, paddings, etc)
- quadkeys and bing maps tile system management in GeoUtils\BingMapsTileSystem