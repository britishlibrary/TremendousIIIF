# TremendousIIIF

A bigly good IIIF image server.

## Features

(or, why another image server)

- Complies with IIIF Image API 2.1, level 2
- Supports additional features from IIIF Image API 2.1 above level 2, such as `square` region, `sizeAboveFull`, `arbitrary` rotation, `mirroring`
- Supports JPG, PNG or WEBP output encoding
- Supports JPEG2000 source, using the Kakadu library 
- Supports TIFF source as a last resort (not recomended)
- Access source images directly over HTTP, or via file system
- Uses the Skia library for image manipulation (as used in Chrome, and is very fast)
- Configuration lets you control things like `sizeAboveFull`, `maxArea`, `maxWidth` or `maxHeight`
- supports jpg, png, webp, pdf output natively, experimental tif and jp2 output (everything above jpg & png configurable)
## Dependencies

.NET 4.6.2

Kakadu 7.10.1, x64 version built against MSVC2017 runtime. kdu_a7AR.dll and kdu_v7AR.dll should be in the *runtime* directory (this is a change in dotnet core 2).

*If you do not have a Kakadu licence, this will not work*

## A note on TIFF support

We don't have any pyramidal tiff as image sources, we almost exclusively use JPEG2000. However, we have a small number of tiff files which are not optimised for delivery at all...

Given how infrequently they are accessed, and the small number of them, it's less effort to simply treat them as though they were. 

We've done some measurements internally and with those images we see a penalty of around 80ms per tile request


## History

The British Library has a long history of working with JPEG2000 and with IIIF in particular, and some of our early experiements are available on github. They were never used in production.

TremendousIIIF is written from scratch, but based on the accumulated knowledge over the years and countless man hours of effort by many colleagues, most of whom are sadly no longer working with us. It is currently used in production.

## Future

Currently builds net462/netstandard2.0/netcoreapp2.0, but not all dependecies are fully compatable (yet)
