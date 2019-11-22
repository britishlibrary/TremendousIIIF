# TremendousIIIF

A bigly good IIIF image server. What's IIIF? Why, it's the International Image Interoperability Framework! https://iiif.io/

## Features

(or, why another image server)

- Complies with IIIF Image API 2.1, level 2
- Complies with IIIF Image API 3.0 (beta), level 2
- Supports additional features from IIIF Image API above level 2, such as `square` region, `sizeAboveFull`, `arbitrary` rotation, `mirroring`
- Supports JPEG2000 source (and output), using the commercial Kakadu library 
- Supports TIFF source as a last resort (not recomended)
- Access source images directly over HTTP (with built in retries), or via file system
- Uses the Skia library for image manipulation (as used in Chrome, and is very fast)
- Configuration lets you control things like `sizeAboveFull`, `maxArea`, `maxWidth` or `maxHeight`
- supports JPG, PNG, WEBP, PDF output natively, experimental TIFF and JP2 output (everything above JPG & PNG configurable)
- Supports reading Geo data from GeoTIFF and JPEG2000 images (if data is stored using the "GeoJP2" method). *Experimental!*

## Dependencies

.NET 4.8 (and therefore Windows, although see the the Future section below for plans)

Kakadu 7.10.7, x64 version built against MSVC2017 runtime. kdu_a7AR.dll and kdu_v7AR.dll should be in the *runtime* directory (this is a change in dotnet core 2).

*If you do not have a Kakadu licence, this will not work*. You will only be able to use TIFF images.

## Configuration

Most settings can be configured through the `appsettings.json` file.

Support for the (still in beta) IIIF Image API 3.0 is enabled by default and can not be switched off. However, it will only be used for `info.json` requests when the `Accept` header includes the v3 profile, e.g. `application/ld+json;profile="http://iiif.io/api/image/3/context.json"`. The _default_ version in all other cases is controlled via configuration, specifically the `ImageServer` section `DefaultAPIVersion` property, which can accept `"v2_1"` or `"v3_0"`.

To specify the location of your source images, use the `"Location"` property in the `ImageServer` section, which accepts file paths (e.g. `"/mnt/nfs/images"` or on windows `"C:\\jp2cache\\"`, note the need to espace slashes in the windows case) or HTTP(s) URI (e.g. `http://192.168.1.22/`)

If allowing upscaling (`sizeAboveFull` in 2.1), you must have either `maxWidth` and `maxHeight` specified, or `maxArea` as per the 3.0 spcification (which is just a sensible clarification). The resizing implementation is much slower when scaling above 1x the size, so it is not recomended to emable this in production at this time.

A health checking interface is provided. Calling `/_monitor` will provide a shallow check that the service is running by returning `200 OK`. By setting an image identifier in `ImageServer.HealthcheckIdentifier`, you can call `/_monitor/deep` to verify the ability to load the source image, again by returning `200 OK` for success or `503 Service Unavailable` otherwise.

To enable Geodata support, you will need an install of the GDAL library (https://gdal.org/), and to set the `ImageServer.GeoDataPath` value in the `appsettings.json` file to the full path. You will also need to set `ImageServer.EnableGeodata` to `true`.

## Extra Information

Additional information can be passed from your proxy server to TremendousIIIF by using HTTP headers. 

`X-maxWidth`, `X-maxHeight` and `X-maxArea` can be supplied to override the values set in `appsettings.json`

`X-PartOf-Manifest` can be set to the identifier of a manifest that an image belongs to. It uses the `ImageServer.ManifestUriFormat` format string in the `appsettings.json` file to format it, so if your proxy provides the full URI, that config value should be set to `"{0}"`.
For 3.0 `info.json` responses it will be used in the `partOf` property.

`X-LicenceUri` can be set to the licence URI for the image. e.g. "http://creativecommons.org/publicdomain/mark/1.0/". The spec(https://preview.iiif.io/api/image-prezi-rc2/api/image/3.0/#56-rights) says:

	The value of this property MUST be a string drawn from the set of Creative Commons license URIs, the RightsStatements.org rights statement URIs, or those added via the Registry of Known Extensions mechanism

If the hostname is not one of the allowed hosts, it is not reflected in the `info.json`. We do not check if it is a valid creative commons or rightsstatements.org URI, only that it is from that domain. For 3.0 `info.json` responses it is the `rights` property.

An example below of a 3.0 `info.json` response with some of these headers submitted in the request

```
{
  "@context": "http://iiif.io/api/image/3/context.json",
  "id": "https://api.bl.uk/image/iiif/ark:/81055/vdc_100022588786.0x000003",
  "type": "ImageService3",
  "protocol": "http://iiif.io/api/image",
  "profile": "level2",
  "width": 2940,
  "height": 4688,
  "maxHeight": 1024,
  "maxWidth": 1024,
  "tiles": [
    {
      "type": "Tile",
      "width": 256,
      "height": 256,
      "scaleFactors": [
        1,
        2,
        4,
        8,
        16,
        32,
        64,
        128,
        256
      ]
    }
  ],
  "extraFormats": [
    "pdf",
    "jp2",
    "webp",
    "tif"
  ],
  "extraFeatures": [
    "rotationArbitrary",
    "mirroring",
    "profileLinkHeader"
  ],
  "partOf": [
    {
      "id": "https://api.bl.uk/metadata/iiif/ark:/81055/tvdc_100004378907.0x000001/manifest.json",
      "type": "Manifest"
    }
  ],
  "rights": "http://creativecommons.org/publicdomain/mark/1.0/"
}
```

## History

The British Library has a long history of working with JPEG2000 and with IIIF in particular, and some of our early experiements are available on github. They were never used in production.

TremendousIIIF is written from scratch, but based on the accumulated knowledge over the years and countless man hours of effort by many colleagues, most of whom are sadly no longer working with us. It is currently used in production.

## Future

Currently builds net48/netcoreapp2.2, but not all dependecies are fully compatable (yet). Kakadu is the blocker, as managed C++ (sometimes referred to a C++/CLI) is not supported in dotnet core. It's quite a large undertaking to generate the Kakadu bindings through P/Invoke as Kakadu uses it's own "hyperdock" system to generate them. 
