# TremendousIIIF

A bigly good IIIF image server.

## Features

(or, why another image server)

- Complies with IIIF Image API 2.1, level 2
- Supports additional features from IIIF Image API 2.1 above level 2, such as `square` region, `sizeAboveFull`, `arbitrary` rotaion, `mirroring`
- Supports JPG, PNG or WEBP output encoding
- Supports JPEG2000 source, using the Kakadu library 
- Supports TIFF source as a last resort (not recomended)
- Access source images directly over HTTP, or via file system
- Uses the Skia library for image manipulation (via the excellent SkiaSharp)
- Configuration lets you control things like `sizeAboveFull`, `maxArea`, `maxWidth` or `maxHeight`

## Dependencies
Kakdu 7.9.1, x64 version against MSVC2015 runtime. kdu_a79R.dll and kdu_v79R.dll should be in C:\Windows\System32

*If you do not have a Kakadu licence, this will not work*

