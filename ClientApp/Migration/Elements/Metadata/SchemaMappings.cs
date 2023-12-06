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
            { "tiff:ImageDescription", SchemaMapping<string>.CreateStandard(MetatagStandards.Standard.Tcat, ThetacatTags.DescriptionTag) },
            { "pse:FileNameOriginal", SchemaMapping<string>.CreateStandard(MetatagStandards.Standard.Pse, PhotoshopElementsTags.FileNameOriginal) },
            { "pse:ImportSourceName", SchemaMapping<string>.CreateStandard(MetatagStandards.Standard.Pse, PhotoshopElementsTags.ImportSourceName) },
            { "pse:ImportSourcePath", SchemaMapping<string>.CreateStandard(MetatagStandards.Standard.Pse, PhotoshopElementsTags.ImportSourcePath) },
            { "exif:Make", SchemaMapping<string>.CreateStandard(MetatagStandards.Standard.Exif, MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagMake) },
            { "exif:Model", SchemaMapping<string>.CreateStandard(MetatagStandards.Standard.Exif, MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagModel) },
            { "pse:TagNotes", SchemaMapping<string>.CreateUser("PseTagNotes", "Tag Notes from PSE") },
        };

    public static readonly Dictionary<string, SchemaMapping<int>> IntMappings =
        new()
        {
            // from metadata_integer_table
            { "tiff:ImageWidth", SchemaMapping<int>.CreateBuiltIn((item, width) => item.ImageWidth = width) },
            { "tiff:ImageHeight", SchemaMapping<int>.CreateBuiltIn((item, height) => item.ImageHeight = height) },
            { "exif:ExposureProgram", SchemaMapping<int>.CreateStandard(MetatagStandards.Standard.Exif, MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagExposureProgram) },
            { "exif:Flash", SchemaMapping<int>.CreateStandard(MetatagStandards.Standard.Exif, MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagFlash) },
            { "exif:ISOSpeedRatings", SchemaMapping<int>.CreateStandard(MetatagStandards.Standard.Exif, MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagIsoSpeed) },
        };

    public static readonly Dictionary<string, SchemaMapping<DateTime>> DateTimeMappings =
        new()
        {
            { "pse:FileDateOriginal", SchemaMapping<DateTime>.CreateBuiltIn((item, dateTime) => item.FileDateOriginal = dateTime) }
        };

    public static readonly Dictionary<string, SchemaMapping<double>> DecimalMappings =
        new()
        {
            { "exif:ExposureBias", SchemaMapping<double>.CreateStandard(MetatagStandards.Standard.Exif, MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagExposureBias) },
            { "exif:ExposureTime", SchemaMapping<double>.CreateStandard(MetatagStandards.Standard.Exif, MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagExposureTime) },
            { "exif:FNumber", SchemaMapping<double>.CreateStandard(MetatagStandards.Standard.Exif, MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagFNumber) },
            { "exif:FocalLength", SchemaMapping<double>.CreateStandard(MetatagStandards.Standard.Exif, MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagFocalLength) },
            { "exif:GPSLatitude", SchemaMapping<double>.CreateStandard(MetatagStandards.Standard.ExifGps, MetadataExtractor.Formats.Exif.GpsDirectory.TagLatitude) },
            { "exif:GPSLongitude", SchemaMapping<double>.CreateStandard(MetatagStandards.Standard.ExifGps, MetadataExtractor.Formats.Exif.GpsDirectory.TagLongitude) },
        };
}
