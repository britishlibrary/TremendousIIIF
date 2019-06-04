# TremendousIIIF

A bigly good IIIF image server.

## Features

(or, why another image server)

- Complies with IIIF Image API 2.1, level 2
- Complies with IIIF Image API 3.0 (RC2)
- Supports additional features from IIIF Image API 2.1 above level 2, such as `square` region, `sizeAboveFull`, `arbitrary` rotation, `mirroring`
- Supports JPG, PNG or WEBP output encoding
- Supports JPEG2000 source (and output), using the commercial Kakadu library 
- Supports TIFF source as a last resort (not recomended)
- Access source images directly over HTTP, or via file system
- Uses the Skia library for image manipulation (as used in Chrome, and is very fast)
- Configuration lets you control things like `sizeAboveFull`, `maxArea`, `maxWidth` or `maxHeight`
- supports JPG, PNG, WEBP, PDF output natively, experimental TIFF and JP2 output (everything above JPG & PNG configurable)
## Dependencies

.NET 4.7.1

Kakadu 7.10.6, x64 version built against MSVC2017 runtime. kdu_a7AR.dll and kdu_v7AR.dll should be in the *runtime* directory (this is a change in dotnet core 2).

*If you do not have a Kakadu licence, this will not work*. You will only be able to use TIFF images.


## History

The British Library has a long history of working with JPEG2000 and with IIIF in particular, and some of our early experiements are available on github. They were never used in production.

TremendousIIIF is written from scratch, but based on the accumulated knowledge over the years and countless man hours of effort by many colleagues, most of whom are sadly no longer working with us. It is currently used in production.

## Future

Currently builds net472/netstandard2.0/netcoreapp2.2, but not all dependecies are fully compatable (yet). Kakadu is the blocker, as managed C++ is not supported in dotnet core.


## Configuration

Support for the (still in beta) IIIF Image API 3.0 is enabled by default and can not be switched off. However, it will only be used for `info.json` requests when the `Accept` header includes the v3 profile, e.g. `application/ld+json;profile="http://iiif.io/api/image/3/context.json"`. The _default_ version in all other cases is controlled via configuration, specifically the `ImageServer` section `DefaultAPIVersion` property, which can accept `"v2_1"` or `"v3_0"`.

To specify the location of your source images, use the `"Location"` property in the `ImageServer` section, which accepts file paths (e.g. `"/mnt/nfs/images"` or on windows `"C:\\jp2cache\\"`, note the need to espace slashes in the windows case) or HTTP URI (e.g. `http://192.168.1.22/`)

If allowing upscaling (`sizeAboveFull` in 2.1), you must have either `maxWidth` and `maxHeight` specified, or `maxArea` as per the 3.0 spcification (which is just a sensible clarification). The resizing implementation is much slower when scaling above 1x the size, so it is not recomended to emable this in production at this time.
