using System;
using System.Collections.Generic;
using System.Windows.Media;
using Thetacat.Model;

namespace Thetacat.Standards;

public class MetatagStandards
{
    public enum Standard
    {
        Unknown,
        Tcat,
        Pse,
        Jpeg,
        Jfif,
        Iptc,
        ExifMakernotes_Nikon1,
        ExifMakernotes_Nikon2,
        Exif,
        ExifGps,
        User,
    };

    public static StandardDefinitions Tcat =
        new(
            Standard.Tcat,
            "TCAT",
            Array.Empty<string>(),
            new Dictionary<int, StandardDefinition>
            {
                { ThetacatTags.DescriptionTag, new(ThetacatTags.DescriptionTag, "Description") }
            });

    public static StandardDefinitions Pse =
        new(
            Standard.Pse,
            "PSE",
            Array.Empty<string>(),
            new Dictionary<int, StandardDefinition>
            {
                { PhotoshopElementsTags.FileNameOriginal, new(PhotoshopElementsTags.FileNameOriginal, "FileNameOriginal")},
                { PhotoshopElementsTags.ImportSourceName, new(PhotoshopElementsTags.ImportSourceName, "ImportSourceName")},
                { PhotoshopElementsTags.ImportSourcePath, new(PhotoshopElementsTags.ImportSourcePath, "ImportSourcePath")}
            });
    //    public static StandardDefinitions  =
    //        new(
    //            "FMT",
    //            new StandardDefinition[]
    //            {
    //                new(MetadataExtractor.Formats., ""),
    //            });

    public static StandardDefinitions Jpeg =
        new(
            Standard.Jpeg,
            "JPEG",
            new[]
            {
                "JpegDirectory"
            },
            new Dictionary<int, StandardDefinition>
            {
                { MetadataExtractor.Formats.Jpeg.JpegDirectory.TagCompressionType, new(MetadataExtractor.Formats.Jpeg.JpegDirectory.TagCompressionType, "CompressionType", false) },
                { MetadataExtractor.Formats.Jpeg.JpegDirectory.TagDataPrecision, new(MetadataExtractor.Formats.Jpeg.JpegDirectory.TagDataPrecision, "DataPrecision", false) },
                { MetadataExtractor.Formats.Jpeg.JpegDirectory.TagImageWidth, new(MetadataExtractor.Formats.Jpeg.JpegDirectory.TagImageWidth, "ImageWidth") },
                { MetadataExtractor.Formats.Jpeg.JpegDirectory.TagImageHeight, new(MetadataExtractor.Formats.Jpeg.JpegDirectory.TagImageHeight, "ImageHeight") },
                { MetadataExtractor.Formats.Jpeg.JpegDirectory.TagNumberOfComponents, new(MetadataExtractor.Formats.Jpeg.JpegDirectory.TagNumberOfComponents, "NumberOfComponents", false) },
                { MetadataExtractor.Formats.Jpeg.JpegDirectory.TagComponentData1, new(MetadataExtractor.Formats.Jpeg.JpegDirectory.TagComponentData1, "ComponentData1", false) },
                { MetadataExtractor.Formats.Jpeg.JpegDirectory.TagComponentData2, new(MetadataExtractor.Formats.Jpeg.JpegDirectory.TagComponentData2, "ComponentData2", false) },
                { MetadataExtractor.Formats.Jpeg.JpegDirectory.TagComponentData3, new(MetadataExtractor.Formats.Jpeg.JpegDirectory.TagComponentData3, "ComponentData3", false) },
                { MetadataExtractor.Formats.Jpeg.JpegDirectory.TagComponentData4, new(MetadataExtractor.Formats.Jpeg.JpegDirectory.TagComponentData4, "ComponentData4", false) }
            });

    public static StandardDefinitions Jfif =
        new(
            Standard.Jfif,
            "JFIF",
            new[]
            {
                "JfifDirectory"
            },
            new Dictionary<int, StandardDefinition>
            {
                { MetadataExtractor.Formats.Jfif.JfifDirectory.TagVersion, new(MetadataExtractor.Formats.Jfif.JfifDirectory.TagVersion, "Version", false)},
                { MetadataExtractor.Formats.Jfif.JfifDirectory.TagUnits, new(MetadataExtractor.Formats.Jfif.JfifDirectory.TagUnits, "Units", false)},
                { MetadataExtractor.Formats.Jfif.JfifDirectory.TagResY, new(MetadataExtractor.Formats.Jfif.JfifDirectory.TagResY, "ResY")},
                { MetadataExtractor.Formats.Jfif.JfifDirectory.TagResX, new(MetadataExtractor.Formats.Jfif.JfifDirectory.TagResX, "ResX")},
                { MetadataExtractor.Formats.Jfif.JfifDirectory.TagThumbWidth, new(MetadataExtractor.Formats.Jfif.JfifDirectory.TagThumbWidth, "ThumbWidth", false)},
                { MetadataExtractor.Formats.Jfif.JfifDirectory.TagThumbHeight, new(MetadataExtractor.Formats.Jfif.JfifDirectory.TagThumbHeight, "ThumbHeight", false)},
            });

    public static StandardDefinitions Iptc =
        new(
            Standard.Iptc,
            "IPTC",
            new[]
            {
                "IptcDirectory"
            },
            new Dictionary<int, StandardDefinition>
            {
                { MetadataExtractor.Formats.Iptc.IptcDirectory.TagEnvelopeRecordVersion, new(MetadataExtractor.Formats.Iptc.IptcDirectory.TagEnvelopeRecordVersion, "EnvelopeRecordVersion", false)},
                { MetadataExtractor.Formats.Iptc.IptcDirectory.TagDestination, new(MetadataExtractor.Formats.Iptc.IptcDirectory.TagDestination, "Destination", false)},
                { MetadataExtractor.Formats.Iptc.IptcDirectory.TagFileFormat, new(MetadataExtractor.Formats.Iptc.IptcDirectory.TagFileFormat, "FileFormat", false)},
                { MetadataExtractor.Formats.Iptc.IptcDirectory.TagFileVersion, new(MetadataExtractor.Formats.Iptc.IptcDirectory.TagFileVersion, "FileVersion", false)},
                { MetadataExtractor.Formats.Iptc.IptcDirectory.TagServiceId, new(MetadataExtractor.Formats.Iptc.IptcDirectory.TagServiceId, "ServiceId", false)},
                { MetadataExtractor.Formats.Iptc.IptcDirectory.TagEnvelopeNumber, new(MetadataExtractor.Formats.Iptc.IptcDirectory.TagEnvelopeNumber, "EnvelopeNumber", false)},
                { MetadataExtractor.Formats.Iptc.IptcDirectory.TagProductId, new(MetadataExtractor.Formats.Iptc.IptcDirectory.TagProductId, "ProductId", false)},
                { MetadataExtractor.Formats.Iptc.IptcDirectory.TagEnvelopePriority, new(MetadataExtractor.Formats.Iptc.IptcDirectory.TagEnvelopePriority, "EnvelopePriority", false)},
                { MetadataExtractor.Formats.Iptc.IptcDirectory.TagDateSent, new(MetadataExtractor.Formats.Iptc.IptcDirectory.TagDateSent, "DateSent", false)},
                { MetadataExtractor.Formats.Iptc.IptcDirectory.TagTimeSent, new(MetadataExtractor.Formats.Iptc.IptcDirectory.TagTimeSent, "TimeSent", false)},
                { MetadataExtractor.Formats.Iptc.IptcDirectory.TagCodedCharacterSet, new(MetadataExtractor.Formats.Iptc.IptcDirectory.TagCodedCharacterSet, "CodedCharacterSet", false)},
                { MetadataExtractor.Formats.Iptc.IptcDirectory.TagUniqueObjectName, new(MetadataExtractor.Formats.Iptc.IptcDirectory.TagUniqueObjectName, "UniqueObjectName", false)},
                { MetadataExtractor.Formats.Iptc.IptcDirectory.TagArmIdentifier, new(MetadataExtractor.Formats.Iptc.IptcDirectory.TagArmIdentifier, "ArmIdentifier", false)},
                { MetadataExtractor.Formats.Iptc.IptcDirectory.TagArmVersion, new(MetadataExtractor.Formats.Iptc.IptcDirectory.TagArmVersion, "ArmVersion", false)},
                { MetadataExtractor.Formats.Iptc.IptcDirectory.TagApplicationRecordVersion, new(MetadataExtractor.Formats.Iptc.IptcDirectory.TagApplicationRecordVersion, "ApplicationRecordVersion", false)},
                { MetadataExtractor.Formats.Iptc.IptcDirectory.TagObjectTypeReference, new(MetadataExtractor.Formats.Iptc.IptcDirectory.TagObjectTypeReference, "ObjectTypeReference", false)},
                { MetadataExtractor.Formats.Iptc.IptcDirectory.TagObjectAttributeReference, new(MetadataExtractor.Formats.Iptc.IptcDirectory.TagObjectAttributeReference, "ObjectAttributeReference", false)},
                { MetadataExtractor.Formats.Iptc.IptcDirectory.TagObjectName, new(MetadataExtractor.Formats.Iptc.IptcDirectory.TagObjectName, "ObjectName", false)},
                { MetadataExtractor.Formats.Iptc.IptcDirectory.TagEditStatus, new(MetadataExtractor.Formats.Iptc.IptcDirectory.TagEditStatus, "EditStatus", false)},
                { MetadataExtractor.Formats.Iptc.IptcDirectory.TagEditorialUpdate, new(MetadataExtractor.Formats.Iptc.IptcDirectory.TagEditorialUpdate, "EditorialUpdate", false)},
                { MetadataExtractor.Formats.Iptc.IptcDirectory.TagUrgency, new(MetadataExtractor.Formats.Iptc.IptcDirectory.TagUrgency, "Urgency", false)},
                { MetadataExtractor.Formats.Iptc.IptcDirectory.TagSubjectReference, new(MetadataExtractor.Formats.Iptc.IptcDirectory.TagSubjectReference, "SubjectReference", false)},
                { MetadataExtractor.Formats.Iptc.IptcDirectory.TagCategory, new(MetadataExtractor.Formats.Iptc.IptcDirectory.TagCategory, "Category", false)},
                { MetadataExtractor.Formats.Iptc.IptcDirectory.TagSupplementalCategories, new(MetadataExtractor.Formats.Iptc.IptcDirectory.TagSupplementalCategories, "SupplementalCategories", false)},
                { MetadataExtractor.Formats.Iptc.IptcDirectory.TagFixtureId, new(MetadataExtractor.Formats.Iptc.IptcDirectory.TagFixtureId, "FixtureId", false)},
                { MetadataExtractor.Formats.Iptc.IptcDirectory.TagKeywords, new(MetadataExtractor.Formats.Iptc.IptcDirectory.TagKeywords, "Keywords", false)},
                { MetadataExtractor.Formats.Iptc.IptcDirectory.TagContentLocationCode, new(MetadataExtractor.Formats.Iptc.IptcDirectory.TagContentLocationCode, "ContentLocationCode", false)},
                { MetadataExtractor.Formats.Iptc.IptcDirectory.TagContentLocationName, new(MetadataExtractor.Formats.Iptc.IptcDirectory.TagContentLocationName, "ContentLocationName", false)},
                { MetadataExtractor.Formats.Iptc.IptcDirectory.TagReleaseDate, new(MetadataExtractor.Formats.Iptc.IptcDirectory.TagReleaseDate, "ReleaseDate", false)},
                { MetadataExtractor.Formats.Iptc.IptcDirectory.TagReleaseTime, new(MetadataExtractor.Formats.Iptc.IptcDirectory.TagReleaseTime, "ReleaseTime", false)},
                { MetadataExtractor.Formats.Iptc.IptcDirectory.TagExpirationDate, new(MetadataExtractor.Formats.Iptc.IptcDirectory.TagExpirationDate, "ExpirationDate", false)},
                { MetadataExtractor.Formats.Iptc.IptcDirectory.TagExpirationTime, new(MetadataExtractor.Formats.Iptc.IptcDirectory.TagExpirationTime, "ExpirationTime", false)},
                { MetadataExtractor.Formats.Iptc.IptcDirectory.TagSpecialInstructions, new(MetadataExtractor.Formats.Iptc.IptcDirectory.TagSpecialInstructions, "SpecialInstructions", false)},
                { MetadataExtractor.Formats.Iptc.IptcDirectory.TagActionAdvised, new(MetadataExtractor.Formats.Iptc.IptcDirectory.TagActionAdvised, "ActionAdvised", false)},
                { MetadataExtractor.Formats.Iptc.IptcDirectory.TagReferenceService, new(MetadataExtractor.Formats.Iptc.IptcDirectory.TagReferenceService, "ReferenceService", false)},
                { MetadataExtractor.Formats.Iptc.IptcDirectory.TagReferenceDate, new(MetadataExtractor.Formats.Iptc.IptcDirectory.TagReferenceDate, "ReferenceDate", false)},
                { MetadataExtractor.Formats.Iptc.IptcDirectory.TagReferenceNumber, new(MetadataExtractor.Formats.Iptc.IptcDirectory.TagReferenceNumber, "ReferenceNumber", false)},
                { MetadataExtractor.Formats.Iptc.IptcDirectory.TagDateCreated, new(MetadataExtractor.Formats.Iptc.IptcDirectory.TagDateCreated, "DateCreated")},
                { MetadataExtractor.Formats.Iptc.IptcDirectory.TagTimeCreated, new(MetadataExtractor.Formats.Iptc.IptcDirectory.TagTimeCreated, "TimeCreated")},
                { MetadataExtractor.Formats.Iptc.IptcDirectory.TagDigitalDateCreated, new(MetadataExtractor.Formats.Iptc.IptcDirectory.TagDigitalDateCreated, "DigitalDateCreated")},
                { MetadataExtractor.Formats.Iptc.IptcDirectory.TagDigitalTimeCreated, new(MetadataExtractor.Formats.Iptc.IptcDirectory.TagDigitalTimeCreated, "DigitalTimeCreated")},
                { MetadataExtractor.Formats.Iptc.IptcDirectory.TagOriginatingProgram, new(MetadataExtractor.Formats.Iptc.IptcDirectory.TagOriginatingProgram, "OriginatingProgram")},
                { MetadataExtractor.Formats.Iptc.IptcDirectory.TagProgramVersion, new(MetadataExtractor.Formats.Iptc.IptcDirectory.TagProgramVersion, "ProgramVersion", false)},
                { MetadataExtractor.Formats.Iptc.IptcDirectory.TagObjectCycle, new(MetadataExtractor.Formats.Iptc.IptcDirectory.TagObjectCycle, "ObjectCycle", false)},
                { MetadataExtractor.Formats.Iptc.IptcDirectory.TagByLine, new(MetadataExtractor.Formats.Iptc.IptcDirectory.TagByLine, "ByLine", false)},
                { MetadataExtractor.Formats.Iptc.IptcDirectory.TagByLineTitle, new(MetadataExtractor.Formats.Iptc.IptcDirectory.TagByLineTitle, "ByLineTitle", false)},
                { MetadataExtractor.Formats.Iptc.IptcDirectory.TagCity, new(MetadataExtractor.Formats.Iptc.IptcDirectory.TagCity, "City")},
                { MetadataExtractor.Formats.Iptc.IptcDirectory.TagSubLocation, new(MetadataExtractor.Formats.Iptc.IptcDirectory.TagSubLocation, "SubLocation")},
                { MetadataExtractor.Formats.Iptc.IptcDirectory.TagProvinceOrState, new(MetadataExtractor.Formats.Iptc.IptcDirectory.TagProvinceOrState, "ProvinceOrState")},
                { MetadataExtractor.Formats.Iptc.IptcDirectory.TagCountryOrPrimaryLocationCode, new(MetadataExtractor.Formats.Iptc.IptcDirectory.TagCountryOrPrimaryLocationCode, "CountryOrPrimaryLocationCode")},
                { MetadataExtractor.Formats.Iptc.IptcDirectory.TagCountryOrPrimaryLocationName, new(MetadataExtractor.Formats.Iptc.IptcDirectory.TagCountryOrPrimaryLocationName, "CountryOrPrimaryLocationName")},
                { MetadataExtractor.Formats.Iptc.IptcDirectory.TagOriginalTransmissionReference, new(MetadataExtractor.Formats.Iptc.IptcDirectory.TagOriginalTransmissionReference, "OriginalTransmissionReference", false)},
                { MetadataExtractor.Formats.Iptc.IptcDirectory.TagHeadline, new(MetadataExtractor.Formats.Iptc.IptcDirectory.TagHeadline, "Headline", false)},
                { MetadataExtractor.Formats.Iptc.IptcDirectory.TagCredit, new(MetadataExtractor.Formats.Iptc.IptcDirectory.TagCredit, "Credit", false)},
                { MetadataExtractor.Formats.Iptc.IptcDirectory.TagSource, new(MetadataExtractor.Formats.Iptc.IptcDirectory.TagSource, "Source", false)},
                { MetadataExtractor.Formats.Iptc.IptcDirectory.TagCopyrightNotice, new(MetadataExtractor.Formats.Iptc.IptcDirectory.TagCopyrightNotice, "CopyrightNotice", false)},
                { MetadataExtractor.Formats.Iptc.IptcDirectory.TagContact, new(MetadataExtractor.Formats.Iptc.IptcDirectory.TagContact, "Contact", false)},
                { MetadataExtractor.Formats.Iptc.IptcDirectory.TagCaption, new(MetadataExtractor.Formats.Iptc.IptcDirectory.TagCaption, "Caption", false)},
                { MetadataExtractor.Formats.Iptc.IptcDirectory.TagLocalCaption, new(MetadataExtractor.Formats.Iptc.IptcDirectory.TagLocalCaption, "LocalCaption", false)},
                { MetadataExtractor.Formats.Iptc.IptcDirectory.TagCaptionWriter, new(MetadataExtractor.Formats.Iptc.IptcDirectory.TagCaptionWriter, "CaptionWriter", false)},
                { MetadataExtractor.Formats.Iptc.IptcDirectory.TagRasterizedCaption, new(MetadataExtractor.Formats.Iptc.IptcDirectory.TagRasterizedCaption, "RasterizedCaption", false)},
                { MetadataExtractor.Formats.Iptc.IptcDirectory.TagImageType, new(MetadataExtractor.Formats.Iptc.IptcDirectory.TagImageType, "ImageType", false)},
                { MetadataExtractor.Formats.Iptc.IptcDirectory.TagImageOrientation, new(MetadataExtractor.Formats.Iptc.IptcDirectory.TagImageOrientation, "ImageOrientation", false)},
                { MetadataExtractor.Formats.Iptc.IptcDirectory.TagLanguageIdentifier, new(MetadataExtractor.Formats.Iptc.IptcDirectory.TagLanguageIdentifier, "LanguageIdentifier", false)},
                { MetadataExtractor.Formats.Iptc.IptcDirectory.TagAudioType, new(MetadataExtractor.Formats.Iptc.IptcDirectory.TagAudioType, "AudioType", false)},
                { MetadataExtractor.Formats.Iptc.IptcDirectory.TagAudioSamplingRate, new(MetadataExtractor.Formats.Iptc.IptcDirectory.TagAudioSamplingRate, "AudioSamplingRate", false)},
                { MetadataExtractor.Formats.Iptc.IptcDirectory.TagAudioSamplingResolution, new(MetadataExtractor.Formats.Iptc.IptcDirectory.TagAudioSamplingResolution, "AudioSamplingResolution", false)},
                { MetadataExtractor.Formats.Iptc.IptcDirectory.TagAudioDuration, new(MetadataExtractor.Formats.Iptc.IptcDirectory.TagAudioDuration, "AudioDuration", false)},
                { MetadataExtractor.Formats.Iptc.IptcDirectory.TagAudioOutcue, new(MetadataExtractor.Formats.Iptc.IptcDirectory.TagAudioOutcue, "AudioOutcue", false)},
                { MetadataExtractor.Formats.Iptc.IptcDirectory.TagJobId, new(MetadataExtractor.Formats.Iptc.IptcDirectory.TagJobId, "JobId", false)},
                { MetadataExtractor.Formats.Iptc.IptcDirectory.TagMasterDocumentId, new(MetadataExtractor.Formats.Iptc.IptcDirectory.TagMasterDocumentId, "MasterDocumentId", false)},
                { MetadataExtractor.Formats.Iptc.IptcDirectory.TagShortDocumentId, new(MetadataExtractor.Formats.Iptc.IptcDirectory.TagShortDocumentId, "ShortDocumentId", false)},
                { MetadataExtractor.Formats.Iptc.IptcDirectory.TagUniqueDocumentId, new(MetadataExtractor.Formats.Iptc.IptcDirectory.TagUniqueDocumentId, "UniqueDocumentId", false)},
                { MetadataExtractor.Formats.Iptc.IptcDirectory.TagOwnerId, new(MetadataExtractor.Formats.Iptc.IptcDirectory.TagOwnerId, "OwnerId", false)},
                { MetadataExtractor.Formats.Iptc.IptcDirectory.TagObjectPreviewFileFormat, new(MetadataExtractor.Formats.Iptc.IptcDirectory.TagObjectPreviewFileFormat, "ObjectPreviewFileFormat", false)},
                { MetadataExtractor.Formats.Iptc.IptcDirectory.TagObjectPreviewFileFormatVersion, new(MetadataExtractor.Formats.Iptc.IptcDirectory.TagObjectPreviewFileFormatVersion, "ObjectPreviewFileFormatVersion", false)},
                { MetadataExtractor.Formats.Iptc.IptcDirectory.TagObjectPreviewData, new(MetadataExtractor.Formats.Iptc.IptcDirectory.TagObjectPreviewData, "ObjectPreviewData", false) },
            });

    public static StandardDefinitions ExifMakernotes_Nikon1 =
        new(
            Standard.ExifMakernotes_Nikon1,
            "EXIF-MakerNotes-Nikon1",
            new[]
            {
                "NikonType1MakernoteDirectory",
            },
            new Dictionary<int, StandardDefinition>
            {
                { MetadataExtractor.Formats.Exif.Makernotes.NikonType1MakernoteDirectory.TagCcdSensitivity, new(MetadataExtractor.Formats.Exif.Makernotes.NikonType1MakernoteDirectory.TagCcdSensitivity, "CcdSensitivity", false)},
                { MetadataExtractor.Formats.Exif.Makernotes.NikonType1MakernoteDirectory.TagColorMode, new(MetadataExtractor.Formats.Exif.Makernotes.NikonType1MakernoteDirectory.TagColorMode, "ColorMode", false)},
                { MetadataExtractor.Formats.Exif.Makernotes.NikonType1MakernoteDirectory.TagDigitalZoom, new(MetadataExtractor.Formats.Exif.Makernotes.NikonType1MakernoteDirectory.TagDigitalZoom, "DigitalZoom", false)},
                { MetadataExtractor.Formats.Exif.Makernotes.NikonType1MakernoteDirectory.TagConverter, new(MetadataExtractor.Formats.Exif.Makernotes.NikonType1MakernoteDirectory.TagConverter, "Converter", false)},
                { MetadataExtractor.Formats.Exif.Makernotes.NikonType1MakernoteDirectory.TagFocus, new(MetadataExtractor.Formats.Exif.Makernotes.NikonType1MakernoteDirectory.TagFocus, "Focus", false)},
                { MetadataExtractor.Formats.Exif.Makernotes.NikonType1MakernoteDirectory.TagImageAdjustment, new(MetadataExtractor.Formats.Exif.Makernotes.NikonType1MakernoteDirectory.TagImageAdjustment, "ImageAdjustment", false)},
                { MetadataExtractor.Formats.Exif.Makernotes.NikonType1MakernoteDirectory.TagQuality, new(MetadataExtractor.Formats.Exif.Makernotes.NikonType1MakernoteDirectory.TagQuality, "Quality", false)},
                { MetadataExtractor.Formats.Exif.Makernotes.NikonType1MakernoteDirectory.TagUnknown1, new(MetadataExtractor.Formats.Exif.Makernotes.NikonType1MakernoteDirectory.TagUnknown1, "Unknown1", false)},
                { MetadataExtractor.Formats.Exif.Makernotes.NikonType1MakernoteDirectory.TagUnknown2, new(MetadataExtractor.Formats.Exif.Makernotes.NikonType1MakernoteDirectory.TagUnknown2, "Unknown2", false)},
                { MetadataExtractor.Formats.Exif.Makernotes.NikonType1MakernoteDirectory.TagUnknown3, new(MetadataExtractor.Formats.Exif.Makernotes.NikonType1MakernoteDirectory.TagUnknown3, "Unknown3", false)},
                { MetadataExtractor.Formats.Exif.Makernotes.NikonType1MakernoteDirectory.TagWhiteBalance, new(MetadataExtractor.Formats.Exif.Makernotes.NikonType1MakernoteDirectory.TagWhiteBalance, "WhiteBalance", false)}
            });

    public static StandardDefinitions ExifMakernotes_Nikon2 =
        new(
            Standard.ExifMakernotes_Nikon2,
            "EXIF-MakerNotes-Nikon2",
            new[]
            {
                "NikonType2MakernoteDirectory",
            },
            new Dictionary<int, StandardDefinition>
            {
                { MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagFirmwareVersion, new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagFirmwareVersion, "FirmwareVersion", false)},
                { MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagIso1, new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagIso1, "Iso1", false)},
                { MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagQualityAndFileFormat, new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagQualityAndFileFormat, "QualityAndFileFormat", false)},
                { MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagCameraWhiteBalance, new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagCameraWhiteBalance, "CameraWhiteBalance", false)},
                { MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagCameraSharpening, new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagCameraSharpening, "CameraSharpening", false)},
                { MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagAfType, new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagAfType, "AfType", false)},
                { MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagCameraWhiteBalanceFine, new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagCameraWhiteBalanceFine, "CameraWhiteBalanceFine", false)},
                { MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagCameraWhiteBalanceRbCoeff, new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagCameraWhiteBalanceRbCoeff, "CameraWhiteBalanceRbCoeff", false)},
                { MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagIsoRequested, new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagIsoRequested, "IsoRequested", false)},
                { MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagIsoMode, new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagIsoMode, "IsoMode", false)},
                { MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagDataDump, new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagDataDump, "DataDump", false)},
                { MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagProgramShift, new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagProgramShift, "ProgramShift", false)},
                { MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagExposureDifference, new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagExposureDifference, "ExposureDifference", false)},
                { MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagPreviewIfd, new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagPreviewIfd, "PreviewIfd", false)},
                { MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagLensType, new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagLensType, "LensType")},
                { MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagFlashUsed, new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagFlashUsed, "FlashUsed")},
                { MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagAfFocusPosition, new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagAfFocusPosition, "AfFocusPosition")},
                { MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagShootingMode, new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagShootingMode, "ShootingMode")},
                { MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagLensStops, new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagLensStops, "LensStops")},
                { MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagContrastCurve, new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagContrastCurve, "ContrastCurve", false)},
                { MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagLightSource, new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagLightSource, "LightSource", false)},
                { MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagShotInfo, new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagShotInfo, "ShotInfo", false)},
                { MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagColorBalance, new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagColorBalance, "ColorBalance", false)},
                { MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagLensData, new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagLensData, "LensData", false)},
                { MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagNefThumbnailSize, new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagNefThumbnailSize, "NefThumbnailSize", false)},
                { MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagSensorPixelSize, new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagSensorPixelSize, "SensorPixelSize", false)},
                { MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagUnknown10, new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagUnknown10, "Unknown10", false)},
                { MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagSceneAssist, new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagSceneAssist, "SceneAssist", false)},
                { MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagDateStampMode, new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagDateStampMode, "DateStampMode", false)},
                { MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagRetouchHistory, new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagRetouchHistory, "RetouchHistory", false)},
                { MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagUnknown12, new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagUnknown12, "Unknown12", false)},
                { MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagFlashSyncMode, new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagFlashSyncMode, "FlashSyncMode", false)},
                { MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagAutoFlashMode, new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagAutoFlashMode, "AutoFlashMode", false)},
                { MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagAutoFlashCompensation, new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagAutoFlashCompensation, "AutoFlashCompensation", false)},
                { MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagExposureSequenceNumber, new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagExposureSequenceNumber, "ExposureSequenceNumber", false)},
                { MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagColorMode, new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagColorMode, "ColorMode", false)},
                { MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagUnknown20, new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagUnknown20, "Unknown20", false)},
                { MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagImageBoundary, new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagImageBoundary, "ImageBoundary", false)},
                { MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagFlashExposureCompensation, new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagFlashExposureCompensation, "FlashExposureCompensation", false)},
                { MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagFlashBracketCompensation, new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagFlashBracketCompensation, "FlashBracketCompensation", false)},
                { MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagAeBracketCompensation, new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagAeBracketCompensation, "AeBracketCompensation", false)},
                { MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagFlashMode, new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagFlashMode, "FlashMode", false)},
                { MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagCropHighSpeed, new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagCropHighSpeed, "CropHighSpeed", false)},
                { MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagExposureTuning, new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagExposureTuning, "ExposureTuning", false)},
                { MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagCameraSerialNumber, new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagCameraSerialNumber, "CameraSerialNumber", false)},
                { MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagColorSpace, new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagColorSpace, "ColorSpace", false)},
                { MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagVrInfo, new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagVrInfo, "VrInfo", false)},
                { MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagImageAuthentication, new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagImageAuthentication, "ImageAuthentication", false)},
                { MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagFaceDetect, new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagFaceDetect, "FaceDetect", false)},
                { MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagActiveDLighting, new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagActiveDLighting, "ActiveDLighting", false)},
                { MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagPictureControl, new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagPictureControl, "PictureControl", false)},
                { MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagWorldTime, new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagWorldTime, "WorldTime", false)},
                { MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagIsoInfo, new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagIsoInfo, "IsoInfo", false)},
                { MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagUnknown36, new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagUnknown36, "Unknown36", false)},
                { MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagUnknown37, new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagUnknown37, "Unknown37", false)},
                { MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagUnknown38, new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagUnknown38, "Unknown38", false)},
                { MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagUnknown39, new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagUnknown39, "Unknown39", false)},
                { MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagVignetteControl, new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagVignetteControl, "VignetteControl", false)},
                { MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagDistortInfo, new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagDistortInfo, "DistortInfo", false)},
                { MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagUnknown41, new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagUnknown41, "Unknown41", false)},
                { MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagUnknown42, new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagUnknown42, "Unknown42", false)},
                { MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagUnknown43, new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagUnknown43, "Unknown43", false)},
                { MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagUnknown44, new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagUnknown44, "Unknown44", false)},
                { MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagUnknown45, new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagUnknown45, "Unknown45", false)},
                { MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagUnknown46, new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagUnknown46, "Unknown46", false)},
                { MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagUnknown47, new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagUnknown47, "Unknown47", false)},
                { MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagSceneMode, new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagSceneMode, "SceneMode", false)},
                { MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagCameraSerialNumber2, new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagCameraSerialNumber2, "CameraSerialNumber2", false)},
                { MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagImageDataSize, new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagImageDataSize, "ImageDataSize", false)},
                { MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagUnknown27, new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagUnknown27, "Unknown27", false)},
                { MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagUnknown28, new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagUnknown28, "Unknown28", false)},
                { MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagImageCount, new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagImageCount, "ImageCount", false)},
                { MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagDeletedImageCount, new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagDeletedImageCount, "DeletedImageCount", false)},
                { MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagSaturation2, new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagSaturation2, "Saturation2", false)},
                { MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagDigitalVariProgram, new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagDigitalVariProgram, "DigitalVariProgram", false)},
                { MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagImageStabilisation, new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagImageStabilisation, "ImageStabilisation", false)},
                { MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagAfResponse, new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagAfResponse, "AfResponse", false)},
                { MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagUnknown29, new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagUnknown29, "Unknown29", false)},
                { MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagUnknown30, new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagUnknown30, "Unknown30", false)},
                { MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagMultiExposure, new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagMultiExposure, "MultiExposure", false)},
                { MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagHighIsoNoiseReduction, new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagHighIsoNoiseReduction, "HighIsoNoiseReduction", false)},
                { MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagUnknown31, new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagUnknown31, "Unknown31", false)},
                { MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagToningEffect, new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagToningEffect, "ToningEffect", false)},
                { MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagUnknown33, new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagUnknown33, "Unknown33", false)},
                { MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagUnknown48, new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagUnknown48, "Unknown48", false)},
                { MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagPowerUpTime, new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagPowerUpTime, "PowerUpTime", false)},
                { MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagAfInfo2, new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagAfInfo2, "AfInfo2", false)},
                { MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagFileInfo, new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagFileInfo, "FileInfo", false)},
                { MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagAfTune, new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagAfTune, "AfTune", false)},
                { MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagFlashInfo, new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagFlashInfo, "FlashInfo", false)},
                { MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagImageOptimisation, new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagImageOptimisation, "ImageOptimisation", false)},
                { MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagImageAdjustment, new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagImageAdjustment, "ImageAdjustment", false)},
                { MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagCameraToneCompensation, new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagCameraToneCompensation, "CameraToneCompensation", false)},
                { MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagAdapter, new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagAdapter, "Adapter", false)},
                { MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagLens, new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagLens, "Lens")},
                { MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagManualFocusDistance, new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagManualFocusDistance, "ManualFocusDistance")},
                { MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagDigitalZoom, new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagDigitalZoom, "DigitalZoom")},
                { MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagCameraColorMode, new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagCameraColorMode, "CameraColorMode", false)},
                { MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagCameraHueAdjustment, new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagCameraHueAdjustment, "CameraHueAdjustment", false)},
                { MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagNefCompression, new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagNefCompression, "NefCompression", false)},
                { MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagSaturation, new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagSaturation, "Saturation", false)},
                { MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagNoiseReduction, new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagNoiseReduction, "NoiseReduction", false)},
                { MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagLinearizationTable, new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagLinearizationTable, "LinearizationTable", false)},
                { MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagNikonCaptureData, new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagNikonCaptureData, "NikonCaptureData", false)},
                { MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagRetouchInfo, new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagRetouchInfo, "RetouchInfo", false)},
                { MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagPictureControl2, new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagPictureControl2, "PictureControl2", false)},
                { MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagUnknown51, new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagUnknown51, "Unknown51", false)},
                { MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagPrintImageMatchingInfo, new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagPrintImageMatchingInfo, "PrintImageMatchingInfo", false)},
                { MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagUnknown52, new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagUnknown52, "Unknown52", false)},
                { MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagUnknown53, new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagUnknown53, "Unknown53", false)},
                { MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagNikonCaptureVersion, new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagNikonCaptureVersion, "NikonCaptureVersion", false)},
                { MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagNikonCaptureOffsets, new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagNikonCaptureOffsets, "NikonCaptureOffsets", false)},
                { MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagNikonScan, new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagNikonScan, "NikonScan", false)},
                { MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagUnknown54, new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagUnknown54, "Unknown54", false)},
                { MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagNefBitDepth, new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagNefBitDepth, "NefBitDepth", false)},
                { MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagUnknown55, new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagUnknown55, "Unknown55", false) },
            });

    //    public static StandardDefinitions Fmt =
    //        new(
    //            "FMT",
    //            new StandardDefinition[]
    //            {
    //                new(MetadataExtractor.Formats., ""),
    //            });

    public static StandardDefinitions Exif =
        new(
            Standard.Exif,
            "EXIF",
            new[]
            {
                "ExifIFD0Directory",
                "ExifInteropDirectory",
                "ExifSubIFDDirectory"
            },
            new Dictionary<int, StandardDefinition>
            {
                { MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagInteropIndex, new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagInteropIndex, "InteropIndex", false)},
                { MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagInteropVersion, new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagInteropVersion, "InteropVersion", false)},
                { MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagNewSubfileType, new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagNewSubfileType, "NewSubfileType", false)},
                { MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagSubfileType, new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagSubfileType, "SubfileType", false)},
                { MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagImageWidth, new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagImageWidth, "ImageWidth")},
                { MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagImageHeight, new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagImageHeight, "ImageHeight")},
                { MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagBitsPerSample, new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagBitsPerSample, "BitsPerSample", false)},
                { MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagCompression, new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagCompression, "Compression", false)},
                { MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagPhotometricInterpretation, new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagPhotometricInterpretation, "PhotometricInterpretation", false)},
                { MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagThresholding, new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagThresholding, "Thresholding", false)},
                { MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagFillOrder, new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagFillOrder, "FillOrder", false)},
                { MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagDocumentName, new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagDocumentName, "DocumentName", false)},
                { MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagImageDescription, new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagImageDescription, "ImageDescription")},
                { MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagMake, new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagMake, "Make")},
                { MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagModel, new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagModel, "Model")},
                { MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagStripOffsets, new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagStripOffsets, "StripOffsets", false)},
                { MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagOrientation, new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagOrientation, "Orientation", false)},
                { MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagSamplesPerPixel, new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagSamplesPerPixel, "SamplesPerPixel", false)},
                { MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagRowsPerStrip, new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagRowsPerStrip, "RowsPerStrip", false)},
                { MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagStripByteCounts, new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagStripByteCounts, "StripByteCounts", false)},
                { MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagMinSampleValue, new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagMinSampleValue, "MinSampleValue", false)},
                { MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagMaxSampleValue, new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagMaxSampleValue, "MaxSampleValue", false)},
                { MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagXResolution, new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagXResolution, "XResolution")},
                { MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagYResolution, new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagYResolution, "YResolution")},
                { MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagPlanarConfiguration, new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagPlanarConfiguration, "PlanarConfiguration", false)},
                { MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagPageName, new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagPageName, "PageName", false)},
                { MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagResolutionUnit, new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagResolutionUnit, "ResolutionUnit", false)},
                { MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagPageNumber, new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagPageNumber, "PageNumber", false)},
                { MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagTransferFunction, new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagTransferFunction, "TransferFunction", false)},
                { MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagSoftware, new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagSoftware, "Software", false)},
                { MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagDateTime, new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagDateTime, "DateTime", false)},
                { MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagArtist, new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagArtist, "Artist", false)},
                { MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagPredictor, new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagPredictor, "Predictor", false)},
                { MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagHostComputer, new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagHostComputer, "HostComputer", false)},
                { MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagWhitePoint, new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagWhitePoint, "WhitePoint", false)},
                { MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagPrimaryChromaticities, new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagPrimaryChromaticities, "PrimaryChromaticities", false)},
                { MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagTileWidth, new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagTileWidth, "TileWidth", false)},
                { MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagTileLength, new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagTileLength, "TileLength", false)},
                { MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagTileOffsets, new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagTileOffsets, "TileOffsets", false)},
                { MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagTileByteCounts, new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagTileByteCounts, "TileByteCounts", false)},
                { MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagSubIfdOffset, new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagSubIfdOffset, "SubIfdOffset", false)},
                { MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagExtraSamples, new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagExtraSamples, "ExtraSamples", false)},
                { MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagSampleFormat, new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagSampleFormat, "SampleFormat", false)},
                { MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagTransferRange, new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagTransferRange, "TransferRange", false)},
                { MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagJpegTables, new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagJpegTables, "JpegTables", false)},
                { MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagJpegProc, new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagJpegProc, "JpegProc", false)},
                { MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagJpegLosslessPredictors, new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagJpegLosslessPredictors, "JpegLosslessPredictors", false)},
                { MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagJpegPointTransforms, new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagJpegPointTransforms, "JpegPointTransforms", false)},
                { MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagJpegQTables, new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagJpegQTables, "JpegQTables", false)},
                { MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagJpegDcTables, new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagJpegDcTables, "JpegDcTables", false)},
                { MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagJpegAcTables, new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagJpegAcTables, "JpegAcTables", false)},
                { MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagYCbCrSubsampling, new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagYCbCrSubsampling, "YCbCrSubsampling", false)},
                { MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagYCbCrPositioning, new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagYCbCrPositioning, "YCbCrPositioning", false)},
                { MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagReferenceBlackWhite, new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagReferenceBlackWhite, "ReferenceBlackWhite", false)},
                { MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagStripRowCounts, new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagStripRowCounts, "StripRowCounts", false)},
                { MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagApplicationNotes, new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagApplicationNotes, "ApplicationNotes", false)},
                { MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagRelatedImageFileFormat, new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagRelatedImageFileFormat, "RelatedImageFileFormat", false)},
                { MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagRelatedImageWidth, new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagRelatedImageWidth, "RelatedImageWidth", false)},
                { MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagRelatedImageHeight, new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagRelatedImageHeight, "RelatedImageHeight", false)},
                { MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagRating, new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagRating, "Rating", false)},
                { MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagRatingPercent, new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagRatingPercent, "RatingPercent", false)},
                { MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagCfaRepeatPatternDim, new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagCfaRepeatPatternDim, "CfaRepeatPatternDim", false)},
                { MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagCfaPattern2, new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagCfaPattern2, "CfaPattern2", false)},
                { MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagBatteryLevel, new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagBatteryLevel, "BatteryLevel", false)},
                { MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagCopyright, new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagCopyright, "Copyright", false)},
                { MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagExposureTime, new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagExposureTime, "ExposureTime")},
                { MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagFNumber, new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagFNumber, "FNumber")},
                { MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagPixelScale, new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagPixelScale, "PixelScale", false)},
                { MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagIptcNaa, new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagIptcNaa, "IptcNaa", false)},
                { MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagModelTiePoint, new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagModelTiePoint, "ModelTiePoint", false)},
                { MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagPhotoshopSettings, new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagPhotoshopSettings, "PhotoshopSettings", false)},
                { MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagInterColorProfile, new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagInterColorProfile, "InterColorProfile", false)},
                { MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagExposureProgram, new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagExposureProgram, "ExposureProgram")},
                { MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagSpectralSensitivity, new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagSpectralSensitivity, "SpectralSensitivity", false)},
                { MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagIsoEquivalent, new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagIsoEquivalent, "IsoEquivalent", false)},
                { MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagOptoElectricConversionFunction, new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagOptoElectricConversionFunction, "OptoElectricConversionFunction", false)},
                { MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagInterlace, new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagInterlace, "Interlace", false)},
                { MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagTimeZoneOffset, new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagTimeZoneOffset, "TimeZoneOffset", false)},
                { MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagSelfTimerMode, new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagSelfTimerMode, "SelfTimerMode", false)},
                { MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagSensitivityType, new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagSensitivityType, "SensitivityType", false)},
                { MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagStandardOutputSensitivity, new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagStandardOutputSensitivity, "StandardOutputSensitivity", false)},
                { MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagRecommendedExposureIndex, new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagRecommendedExposureIndex, "RecommendedExposureIndex", false)},
                { MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagIsoSpeed, new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagIsoSpeed, "IsoSpeed")},
                { MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagIsoSpeedLatitudeYYY, new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagIsoSpeedLatitudeYYY, "IsoSpeedLatitudeYYY", false)},
                { MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagIsoSpeedLatitudeZZZ, new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagIsoSpeedLatitudeZZZ, "IsoSpeedLatitudeZZZ", false)},
                { MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagExifVersion, new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagExifVersion, "ExifVersion", false)},
                { MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagDateTimeOriginal, new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagDateTimeOriginal, "DateTimeOriginal", false)},
                { MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagDateTimeDigitized, new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagDateTimeDigitized, "DateTimeDigitized", false)},
                { MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagTimeZone, new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagTimeZone, "TimeZone", false)},
                { MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagTimeZoneOriginal, new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagTimeZoneOriginal, "TimeZoneOriginal", false)},
                { MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagTimeZoneDigitized, new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagTimeZoneDigitized, "TimeZoneDigitized", false)},
                { MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagComponentsConfiguration, new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagComponentsConfiguration, "ComponentsConfiguration", false)},
                { MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagCompressedAverageBitsPerPixel, new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagCompressedAverageBitsPerPixel, "CompressedAverageBitsPerPixel", false)},
                { MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagShutterSpeed, new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagShutterSpeed, "ShutterSpeed", false)},
                { MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagAperture, new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagAperture, "Aperture")},
                { MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagBrightnessValue, new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagBrightnessValue, "BrightnessValue", false)},
                { MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagExposureBias, new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagExposureBias, "ExposureBias")},
                { MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagMaxAperture, new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagMaxAperture, "MaxAperture", false)},
                { MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagSubjectDistance, new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagSubjectDistance, "SubjectDistance", false)},
                { MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagMeteringMode, new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagMeteringMode, "MeteringMode", false)},
                { MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagWhiteBalance, new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagWhiteBalance, "WhiteBalance", false)},
                { MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagFlash, new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagFlash, "Flash")},
                { MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagFocalLength, new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagFocalLength, "FocalLength")},
                { MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagFlashEnergyTiffEp, new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagFlashEnergyTiffEp, "FlashEnergyTiffEp", false)},
                { MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagSpatialFreqResponseTiffEp, new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagSpatialFreqResponseTiffEp, "SpatialFreqResponseTiffEp", false)},
                { MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagNoise, new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagNoise, "Noise", false)},
                { MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagFocalPlaneXResolutionTiffEp, new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagFocalPlaneXResolutionTiffEp, "FocalPlaneXResolutionTiffEp", false)},
                { MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagFocalPlaneYResolutionTiffEp, new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagFocalPlaneYResolutionTiffEp, "FocalPlaneYResolutionTiffEp", false)},
                { MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagImageNumber, new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagImageNumber, "ImageNumber", false)},
                { MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagSecurityClassification, new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagSecurityClassification, "SecurityClassification", false)},
                { MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagImageHistory, new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagImageHistory, "ImageHistory", false)},
                { MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagSubjectLocationTiffEp, new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagSubjectLocationTiffEp, "SubjectLocationTiffEp", false)},
                { MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagExposureIndexTiffEp, new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagExposureIndexTiffEp, "ExposureIndexTiffEp", false)},
                { MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagStandardIdTiffEp, new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagStandardIdTiffEp, "StandardIdTiffEp", false)},
                { MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagMakernote, new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagMakernote, "Makernote", false)},
                { MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagUserComment, new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagUserComment, "UserComment")},
                { MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagSubsecondTime, new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagSubsecondTime, "SubsecondTime", false)},
                { MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagSubsecondTimeOriginal, new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagSubsecondTimeOriginal, "SubsecondTimeOriginal", false)},
                { MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagSubsecondTimeDigitized, new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagSubsecondTimeDigitized, "SubsecondTimeDigitized", false)},
                { MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagWinTitle, new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagWinTitle, "WinTitle", false)},
                { MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagWinComment, new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagWinComment, "WinComment", false)},
                { MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagWinAuthor, new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagWinAuthor, "WinAuthor", false)},
                { MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagWinKeywords, new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagWinKeywords, "WinKeywords", false)},
                { MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagWinSubject, new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagWinSubject, "WinSubject", false)},
                { MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagFlashpixVersion, new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagFlashpixVersion, "FlashpixVersion", false)},
                { MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagColorSpace, new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagColorSpace, "ColorSpace", false)},
                { MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagExifImageWidth, new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagExifImageWidth, "ExifImageWidth", false)},
                { MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagExifImageHeight, new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagExifImageHeight, "ExifImageHeight", false)},
                { MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagRelatedSoundFile, new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagRelatedSoundFile, "RelatedSoundFile", false)},
                { MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagFlashEnergy, new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagFlashEnergy, "FlashEnergy", false)},
                { MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagSpatialFreqResponse, new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagSpatialFreqResponse, "SpatialFreqResponse", false)},
                { MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagFocalPlaneXResolution, new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagFocalPlaneXResolution, "FocalPlaneXResolution", false)},
                { MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagFocalPlaneYResolution, new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagFocalPlaneYResolution, "FocalPlaneYResolution", false)},
                { MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagFocalPlaneResolutionUnit, new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagFocalPlaneResolutionUnit, "FocalPlaneResolutionUnit", false)},
                { MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagSubjectLocation, new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagSubjectLocation, "SubjectLocation", false)},
                { MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagExposureIndex, new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagExposureIndex, "ExposureIndex", false)},
                { MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagSensingMethod, new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagSensingMethod, "SensingMethod", false)},
                { MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagFileSource, new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagFileSource, "FileSource", false)},
                { MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagSceneType, new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagSceneType, "SceneType", false)},
                { MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagCfaPattern, new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagCfaPattern, "CfaPattern", false)},
                { MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagCustomRendered, new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagCustomRendered, "CustomRendered", false)},
                { MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagExposureMode, new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagExposureMode, "ExposureMode", false)},
                { MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagWhiteBalanceMode, new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagWhiteBalanceMode, "WhiteBalanceMode", false)},
                { MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagDigitalZoomRatio, new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagDigitalZoomRatio, "DigitalZoomRatio", false)},
                { MetadataExtractor.Formats.Exif.ExifDirectoryBase.Tag35MMFilmEquivFocalLength, new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.Tag35MMFilmEquivFocalLength, "35MMFilmEquivFocalLength")},
                { MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagSceneCaptureType, new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagSceneCaptureType, "SceneCaptureType", false)},
                { MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagGainControl, new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagGainControl, "GainControl", false)},
                { MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagContrast, new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagContrast, "Contrast", false)},
                { MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagSaturation, new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagSaturation, "Saturation", false)},
                { MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagSharpness, new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagSharpness, "Sharpness", false)},
                { MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagDeviceSettingDescription, new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagDeviceSettingDescription, "DeviceSettingDescription", false)},
                { MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagSubjectDistanceRange, new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagSubjectDistanceRange, "SubjectDistanceRange", false)},
                { MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagImageUniqueId, new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagImageUniqueId, "ImageUniqueId", false)},
                { MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagCameraOwnerName, new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagCameraOwnerName, "CameraOwnerName", false)},
                { MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagBodySerialNumber, new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagBodySerialNumber, "BodySerialNumber", false)},
                { MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagLensSpecification, new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagLensSpecification, "LensSpecification")},
                { MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagLensMake, new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagLensMake, "LensMake")},
                { MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagLensModel, new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagLensModel, "LensModel")},
                { MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagLensSerialNumber, new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagLensSerialNumber, "LensSerialNumber")},
                { MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagGdalMetadata, new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagGdalMetadata, "GdalMetadata", false)},
                { MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagGdalNoData, new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagGdalNoData, "GdalNoData", false)},
                { MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagGamma, new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagGamma, "Gamma", false)},
                { MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagPrintImageMatchingInfo, new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagPrintImageMatchingInfo, "PrintImageMatchingInfo", false)},
                { MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagPanasonicTitle, new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagPanasonicTitle, "PanasonicTitle", false)},
                { MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagPanasonicTitle2, new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagPanasonicTitle2, "PanasonicTitle2", false)},
                { MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagPadding, new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagPadding, "Padding", false)},
                { MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagLens, new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagLens, "Lens") },
            });

    public static StandardDefinitions ExifGps =
        new(
            Standard.ExifGps,
            "EXIF",
            new[]
            {
                "GpsDirectory",
            },
            new Dictionary<int, StandardDefinition>
            {
                { MetadataExtractor.Formats.Exif.GpsDirectory.TagVersionId, new(MetadataExtractor.Formats.Exif.GpsDirectory.TagVersionId, "VersionId", false)},
                { MetadataExtractor.Formats.Exif.GpsDirectory.TagLatitudeRef, new(MetadataExtractor.Formats.Exif.GpsDirectory.TagLatitudeRef, "LatitudeRef")},
                { MetadataExtractor.Formats.Exif.GpsDirectory.TagLatitude, new(MetadataExtractor.Formats.Exif.GpsDirectory.TagLatitude, "Latitude")},
                { MetadataExtractor.Formats.Exif.GpsDirectory.TagLongitudeRef, new(MetadataExtractor.Formats.Exif.GpsDirectory.TagLongitudeRef, "LongitudeRef")},
                { MetadataExtractor.Formats.Exif.GpsDirectory.TagLongitude, new(MetadataExtractor.Formats.Exif.GpsDirectory.TagLongitude, "Longitude")},
                { MetadataExtractor.Formats.Exif.GpsDirectory.TagAltitudeRef, new(MetadataExtractor.Formats.Exif.GpsDirectory.TagAltitudeRef, "AltitudeRef")},
                { MetadataExtractor.Formats.Exif.GpsDirectory.TagAltitude, new(MetadataExtractor.Formats.Exif.GpsDirectory.TagAltitude, "Altitude")},
                { MetadataExtractor.Formats.Exif.GpsDirectory.TagTimeStamp, new(MetadataExtractor.Formats.Exif.GpsDirectory.TagTimeStamp, "TimeStamp", false)},
                { MetadataExtractor.Formats.Exif.GpsDirectory.TagSatellites, new(MetadataExtractor.Formats.Exif.GpsDirectory.TagSatellites, "Satellites", false)},
                { MetadataExtractor.Formats.Exif.GpsDirectory.TagStatus, new(MetadataExtractor.Formats.Exif.GpsDirectory.TagStatus, "Status", false)},
                { MetadataExtractor.Formats.Exif.GpsDirectory.TagMeasureMode, new(MetadataExtractor.Formats.Exif.GpsDirectory.TagMeasureMode, "MeasureMode", false)},
                { MetadataExtractor.Formats.Exif.GpsDirectory.TagDop, new(MetadataExtractor.Formats.Exif.GpsDirectory.TagDop, "Dop", false)},
                { MetadataExtractor.Formats.Exif.GpsDirectory.TagSpeedRef, new(MetadataExtractor.Formats.Exif.GpsDirectory.TagSpeedRef, "SpeedRef", false)},
                { MetadataExtractor.Formats.Exif.GpsDirectory.TagSpeed, new(MetadataExtractor.Formats.Exif.GpsDirectory.TagSpeed, "Speed", false)},
                { MetadataExtractor.Formats.Exif.GpsDirectory.TagTrackRef, new(MetadataExtractor.Formats.Exif.GpsDirectory.TagTrackRef, "TrackRef", false)},
                { MetadataExtractor.Formats.Exif.GpsDirectory.TagTrack, new(MetadataExtractor.Formats.Exif.GpsDirectory.TagTrack, "Track", false)},
                { MetadataExtractor.Formats.Exif.GpsDirectory.TagImgDirectionRef, new(MetadataExtractor.Formats.Exif.GpsDirectory.TagImgDirectionRef, "ImgDirectionRef", false)},
                { MetadataExtractor.Formats.Exif.GpsDirectory.TagImgDirection, new(MetadataExtractor.Formats.Exif.GpsDirectory.TagImgDirection, "ImgDirection", false)},
                { MetadataExtractor.Formats.Exif.GpsDirectory.TagMapDatum, new(MetadataExtractor.Formats.Exif.GpsDirectory.TagMapDatum, "MapDatum", false)},
                { MetadataExtractor.Formats.Exif.GpsDirectory.TagDestLatitudeRef, new(MetadataExtractor.Formats.Exif.GpsDirectory.TagDestLatitudeRef, "DestLatitudeRef", false)},
                { MetadataExtractor.Formats.Exif.GpsDirectory.TagDestLatitude, new(MetadataExtractor.Formats.Exif.GpsDirectory.TagDestLatitude, "DestLatitude", false)},
                { MetadataExtractor.Formats.Exif.GpsDirectory.TagDestLongitudeRef, new(MetadataExtractor.Formats.Exif.GpsDirectory.TagDestLongitudeRef, "DestLongitudeRef", false)},
                { MetadataExtractor.Formats.Exif.GpsDirectory.TagDestLongitude, new(MetadataExtractor.Formats.Exif.GpsDirectory.TagDestLongitude, "DestLongitude", false)},
                { MetadataExtractor.Formats.Exif.GpsDirectory.TagDestBearingRef, new(MetadataExtractor.Formats.Exif.GpsDirectory.TagDestBearingRef, "DestBearingRef", false)},
                { MetadataExtractor.Formats.Exif.GpsDirectory.TagDestBearing, new(MetadataExtractor.Formats.Exif.GpsDirectory.TagDestBearing, "DestBearing", false)},
                { MetadataExtractor.Formats.Exif.GpsDirectory.TagDestDistanceRef, new(MetadataExtractor.Formats.Exif.GpsDirectory.TagDestDistanceRef, "DestDistanceRef", false)},
                { MetadataExtractor.Formats.Exif.GpsDirectory.TagDestDistance, new(MetadataExtractor.Formats.Exif.GpsDirectory.TagDestDistance, "DestDistance", false)},
                { MetadataExtractor.Formats.Exif.GpsDirectory.TagProcessingMethod, new(MetadataExtractor.Formats.Exif.GpsDirectory.TagProcessingMethod, "ProcessingMethod", false)},
                { MetadataExtractor.Formats.Exif.GpsDirectory.TagAreaInformation, new(MetadataExtractor.Formats.Exif.GpsDirectory.TagAreaInformation, "AreaInformation", false)},
                { MetadataExtractor.Formats.Exif.GpsDirectory.TagDateStamp, new(MetadataExtractor.Formats.Exif.GpsDirectory.TagDateStamp, "DateStamp", false)},
                { MetadataExtractor.Formats.Exif.GpsDirectory.TagDifferential, new(MetadataExtractor.Formats.Exif.GpsDirectory.TagDifferential, "Differential", false)},
                { MetadataExtractor.Formats.Exif.GpsDirectory.TagHPositioningError, new(MetadataExtractor.Formats.Exif.GpsDirectory.TagHPositioningError, "HPositioningError", false)},
            });

    public static Dictionary<Standard, StandardDefinitions> KnownStandards =
        new()
        {
            { Standard.Tcat, Tcat },
            { Standard.Pse, Pse },
            { Standard.Jpeg, Jpeg },
            { Standard.Jfif, Jfif },
            { Standard.Iptc, Iptc },
            { Standard.ExifMakernotes_Nikon1, ExifMakernotes_Nikon1 },
            { Standard.ExifMakernotes_Nikon2, ExifMakernotes_Nikon2 },
            { Standard.Exif, Exif },
            { Standard.ExifGps, ExifGps },
        };

    public static Standard GetStandardFromType(string typeName)
    {
        foreach (Standard standard in KnownStandards.Keys)
        {
            StandardDefinitions definitions = KnownStandards[standard];

            foreach (string standardTypeName in definitions.TypeNames)
            {
                if (string.Compare(standardTypeName, typeName, StringComparison.InvariantCultureIgnoreCase) == 0)
                    return standard;
            }
        }

        return Standard.Unknown;
    }

    /*----------------------------------------------------------------------------
        %%Function: GetStandardMappingsFromType
        %%Qualified: Thetacat.Standards.MetatagStandards.GetStandardMappingsFromType

    ----------------------------------------------------------------------------*/
    public static StandardDefinitions? GetStandardMappingsFromType(string typeName)
    {
        Standard standard = GetStandardFromType(typeName);
        if (standard == Standard.Unknown)
            return null;

        return KnownStandards[standard];
    }

    /*----------------------------------------------------------------------------
        %%Function: GetStandardMappings
        %%Qualified: Thetacat.Standards.MetatagStandards.GetStandardMappings
      
    ----------------------------------------------------------------------------*/
    public static StandardDefinitions GetStandardMappings(Standard standard)
    {
        if (!KnownStandards.ContainsKey(standard))
            throw new Exception($"unknown standard standard: ${standard}");

        return KnownStandards[standard];
    }

    public static List<StandardDefinitions> GetStandardMappingsFromStandardName(string standardName)
    {
        List<StandardDefinitions> standardMappings = new();

        foreach (StandardDefinitions standard in KnownStandards.Values)
        {
            if (string.Compare(standard.Tag, standardName, StringComparison.InvariantCultureIgnoreCase) == 0)
                standardMappings.Add(standard);
        }

        return standardMappings;
    }

    public static Standard GetStandardFromStandardTag(string standardTag)
    {
        foreach (Standard standard in KnownStandards.Keys)
        {
            StandardDefinitions definitions = KnownStandards[standard];

            if (definitions.Tag == standardTag)
                return standard;
        }

        return Standard.Unknown;
    }

    public static string GetStandardsTagFromStandard(Standard standard)
    {
        if (standard == Standard.Unknown)
            return string.Empty;

        if (standard == Standard.User)
            return "user";

        return KnownStandards[standard].Tag;
    }

}
