using System;
using System.Collections.Generic;
using Thetacat.Standards;

namespace Thetacat.Migration.Elements.Metadata;

public class SchemaMappings
{
    public static readonly Dictionary<string, SchemaMapping<string>> StringMappings =
        new()
        {
            // from metadata_string_table
            { "tiff:ImageDescription", SchemaMapping<string>.CreateStandard(StandardsMappings.Tcat.Tag, ThetacatTags.DescriptionTag) },
            { "pse:FileNameOriginal", SchemaMapping<string>.CreateStandard(StandardsMappings.Pse.Tag, PhotoshopElements.FileNameOriginal) },
            { "pse:ImportSourceName", SchemaMapping<string>.CreateStandard(StandardsMappings.Pse.Tag, PhotoshopElements.FileNameOriginal) },
            { "pse:ImportSourcePath", SchemaMapping<string>.CreateStandard(StandardsMappings.Pse.Tag, PhotoshopElements.FileNameOriginal) },
            { "exif:Make", SchemaMapping<string>.CreateStandard(StandardsMappings.Exif.Tag, MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagMake) },
            { "exif:Model", SchemaMapping<string>.CreateStandard(StandardsMappings.Exif.Tag, MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagModel) },
            { "pse:TagNotes", SchemaMapping<string>.CreateUser("PseTagNotes", "Tag Notes from PSE") },
        };

    public static readonly Dictionary<string, SchemaMapping<int>> IntMappings =
        new()
        {
            // from metadata_integer_table
            { "tiff:ImageWidth", SchemaMapping<int>.CreateBuiltIn((item, width) => item.ImageWidth = width) },
            { "tiff:ImageHeight", SchemaMapping<int>.CreateBuiltIn((item, height) => item.ImageHeight = height) },
            { "exif:ExposureProgram", SchemaMapping<int>.CreateStandard(StandardsMappings.Exif.Tag, MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagExposureProgram) },
            { "exif:Flash", SchemaMapping<int>.CreateStandard(StandardsMappings.Exif.Tag, MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagFlash) },
            { "exif:ISOSpeedRatings", SchemaMapping<int>.CreateStandard(StandardsMappings.Exif.Tag, MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagIsoSpeed) },
        };

    public static readonly Dictionary<string, SchemaMapping<DateTime>> DateTimeMappings =
        new()
        {
            { "pse:FileDateOriginal", SchemaMapping<DateTime>.CreateBuiltIn((item, dateTime) => item.FileDateOriginal = dateTime) }
        };

    public static readonly Dictionary<string, SchemaMapping<double>> DecimalMappings =
        new()
        {
            { "exif:ExposureBias", SchemaMapping<double>.CreateStandard(StandardsMappings.Exif.Tag, MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagExposureBias) },
            { "exif:ExposureTime", SchemaMapping<double>.CreateStandard(StandardsMappings.Exif.Tag, MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagExposureTime) },
            { "exif:FNumber", SchemaMapping<double>.CreateStandard(StandardsMappings.Exif.Tag, MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagFNumber) },
            { "exif:FocalLength", SchemaMapping<double>.CreateStandard(StandardsMappings.Exif.Tag, MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagFocalLength) },
            { "exif:GPSLatitude", SchemaMapping<double>.CreateStandard(StandardsMappings.ExifGps.Tag, MetadataExtractor.Formats.Exif.GpsDirectory.TagLatitude) },
            { "exif:GPSLongitude", SchemaMapping<double>.CreateStandard(StandardsMappings.ExifGps.Tag, MetadataExtractor.Formats.Exif.GpsDirectory.TagLongitude) },
        };
}
