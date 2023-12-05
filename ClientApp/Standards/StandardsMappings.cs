using System;
using Thetacat.Model;

namespace Thetacat.Standards;

public class StandardsMappings
{
    public static StandardMappings Tcat =
        new (
            "TCAT",
            Array.Empty<string>(),
            new StandardMapping[]
            {
                new (ThetacatTags.DescriptionTag, "Description")
            });

    public static StandardMappings Pse =
        new(
            "PSE",
            Array.Empty<string>(),
            new StandardMapping[]
            {
                new(PhotoshopElements.FileNameOriginal, "FileNameOriginal"),
                new(PhotoshopElements.ImportSourceName, "ImportSourceName"),
                new(PhotoshopElements.ImportSourcePath, "ImportSourcePath")
            });

    //    public static StandardMappings  =
    //        new(
    //            "FMT",
    //            new StandardMapping[]
    //            {
    //                new(MetadataExtractor.Formats., ""),
    //            });

    public static StandardMappings Jpeg =
        new(
            "JPEG",
            new[]
            {
                "JpegDirectory"
            },
            new StandardMapping[]
            {
                new(MetadataExtractor.Formats.Jpeg.JpegDirectory.TagCompressionType, "CompressionType", false),
                new(MetadataExtractor.Formats.Jpeg.JpegDirectory.TagDataPrecision, "DataPrecision", false),
                new(MetadataExtractor.Formats.Jpeg.JpegDirectory.TagImageWidth, "ImageWidth"),
                new(MetadataExtractor.Formats.Jpeg.JpegDirectory.TagImageHeight, "ImageHeight"),
                new(MetadataExtractor.Formats.Jpeg.JpegDirectory.TagNumberOfComponents, "NumberOfComponents", false),
                new(MetadataExtractor.Formats.Jpeg.JpegDirectory.TagComponentData1, "ComponentData1", false),
                new(MetadataExtractor.Formats.Jpeg.JpegDirectory.TagComponentData2, "ComponentData2", false),
                new(MetadataExtractor.Formats.Jpeg.JpegDirectory.TagComponentData3, "ComponentData3", false),
                new(MetadataExtractor.Formats.Jpeg.JpegDirectory.TagComponentData4, "ComponentData4", false),
            });

    public static StandardMappings Jfif =
        new(
            "JFIF",
            new[]
            {
                "JfifDirectory"
            },
            new StandardMapping[]
            {
                new(MetadataExtractor.Formats.Jfif.JfifDirectory.TagVersion, "Version", false),
                new(MetadataExtractor.Formats.Jfif.JfifDirectory.TagUnits, "Units", false),
                new(MetadataExtractor.Formats.Jfif.JfifDirectory.TagResY, "ResY"),
                new(MetadataExtractor.Formats.Jfif.JfifDirectory.TagResX, "ResX"),
                new(MetadataExtractor.Formats.Jfif.JfifDirectory.TagThumbWidth, "ThumbWidth", false),
                new(MetadataExtractor.Formats.Jfif.JfifDirectory.TagThumbHeight, "ThumbHeight", false)
            });

    public static StandardMappings Iptc =
        new(
            "IPTC",
            new[]
            {
                "IptcDirectory"
            },
            new StandardMapping[]
            {
                new(MetadataExtractor.Formats.Iptc.IptcDirectory.TagEnvelopeRecordVersion, "EnvelopeRecordVersion", false),
                new(MetadataExtractor.Formats.Iptc.IptcDirectory.TagDestination, "Destination", false),
                new(MetadataExtractor.Formats.Iptc.IptcDirectory.TagFileFormat, "FileFormat", false),
                new(MetadataExtractor.Formats.Iptc.IptcDirectory.TagFileVersion, "FileVersion", false),
                new(MetadataExtractor.Formats.Iptc.IptcDirectory.TagServiceId, "ServiceId", false),
                new(MetadataExtractor.Formats.Iptc.IptcDirectory.TagEnvelopeNumber, "EnvelopeNumber", false),
                new(MetadataExtractor.Formats.Iptc.IptcDirectory.TagProductId, "ProductId", false),
                new(MetadataExtractor.Formats.Iptc.IptcDirectory.TagEnvelopePriority, "EnvelopePriority", false),
                new(MetadataExtractor.Formats.Iptc.IptcDirectory.TagDateSent, "DateSent", false),
                new(MetadataExtractor.Formats.Iptc.IptcDirectory.TagTimeSent, "TimeSent", false),
                new(MetadataExtractor.Formats.Iptc.IptcDirectory.TagCodedCharacterSet, "CodedCharacterSet", false),
                new(MetadataExtractor.Formats.Iptc.IptcDirectory.TagUniqueObjectName, "UniqueObjectName", false),
                new(MetadataExtractor.Formats.Iptc.IptcDirectory.TagArmIdentifier, "ArmIdentifier", false),
                new(MetadataExtractor.Formats.Iptc.IptcDirectory.TagArmVersion, "ArmVersion", false),
                new(MetadataExtractor.Formats.Iptc.IptcDirectory.TagApplicationRecordVersion, "ApplicationRecordVersion", false),
                new(MetadataExtractor.Formats.Iptc.IptcDirectory.TagObjectTypeReference, "ObjectTypeReference", false),
                new(MetadataExtractor.Formats.Iptc.IptcDirectory.TagObjectAttributeReference, "ObjectAttributeReference", false),
                new(MetadataExtractor.Formats.Iptc.IptcDirectory.TagObjectName, "ObjectName", false),
                new(MetadataExtractor.Formats.Iptc.IptcDirectory.TagEditStatus, "EditStatus", false),
                new(MetadataExtractor.Formats.Iptc.IptcDirectory.TagEditorialUpdate, "EditorialUpdate", false),
                new(MetadataExtractor.Formats.Iptc.IptcDirectory.TagUrgency, "Urgency", false),
                new(MetadataExtractor.Formats.Iptc.IptcDirectory.TagSubjectReference, "SubjectReference", false),
                new(MetadataExtractor.Formats.Iptc.IptcDirectory.TagCategory, "Category", false),
                new(MetadataExtractor.Formats.Iptc.IptcDirectory.TagSupplementalCategories, "SupplementalCategories", false),
                new(MetadataExtractor.Formats.Iptc.IptcDirectory.TagFixtureId, "FixtureId", false),
                new(MetadataExtractor.Formats.Iptc.IptcDirectory.TagKeywords, "Keywords", false),
                new(MetadataExtractor.Formats.Iptc.IptcDirectory.TagContentLocationCode, "ContentLocationCode", false),
                new(MetadataExtractor.Formats.Iptc.IptcDirectory.TagContentLocationName, "ContentLocationName", false),
                new(MetadataExtractor.Formats.Iptc.IptcDirectory.TagReleaseDate, "ReleaseDate", false),
                new(MetadataExtractor.Formats.Iptc.IptcDirectory.TagReleaseTime, "ReleaseTime", false),
                new(MetadataExtractor.Formats.Iptc.IptcDirectory.TagExpirationDate, "ExpirationDate", false),
                new(MetadataExtractor.Formats.Iptc.IptcDirectory.TagExpirationTime, "ExpirationTime", false),
                new(MetadataExtractor.Formats.Iptc.IptcDirectory.TagSpecialInstructions, "SpecialInstructions", false),
                new(MetadataExtractor.Formats.Iptc.IptcDirectory.TagActionAdvised, "ActionAdvised", false),
                new(MetadataExtractor.Formats.Iptc.IptcDirectory.TagReferenceService, "ReferenceService", false),
                new(MetadataExtractor.Formats.Iptc.IptcDirectory.TagReferenceDate, "ReferenceDate", false),
                new(MetadataExtractor.Formats.Iptc.IptcDirectory.TagReferenceNumber, "ReferenceNumber", false),
                new(MetadataExtractor.Formats.Iptc.IptcDirectory.TagDateCreated, "DateCreated"),
                new(MetadataExtractor.Formats.Iptc.IptcDirectory.TagTimeCreated, "TimeCreated"),
                new(MetadataExtractor.Formats.Iptc.IptcDirectory.TagDigitalDateCreated, "DigitalDateCreated"),
                new(MetadataExtractor.Formats.Iptc.IptcDirectory.TagDigitalTimeCreated, "DigitalTimeCreated"),
                new(MetadataExtractor.Formats.Iptc.IptcDirectory.TagOriginatingProgram, "OriginatingProgram"),
                new(MetadataExtractor.Formats.Iptc.IptcDirectory.TagProgramVersion, "ProgramVersion", false),
                new(MetadataExtractor.Formats.Iptc.IptcDirectory.TagObjectCycle, "ObjectCycle", false),
                new(MetadataExtractor.Formats.Iptc.IptcDirectory.TagByLine, "ByLine", false),
                new(MetadataExtractor.Formats.Iptc.IptcDirectory.TagByLineTitle, "ByLineTitle", false),
                new(MetadataExtractor.Formats.Iptc.IptcDirectory.TagCity, "City"),
                new(MetadataExtractor.Formats.Iptc.IptcDirectory.TagSubLocation, "SubLocation"),
                new(MetadataExtractor.Formats.Iptc.IptcDirectory.TagProvinceOrState, "ProvinceOrState"),
                new(MetadataExtractor.Formats.Iptc.IptcDirectory.TagCountryOrPrimaryLocationCode, "CountryOrPrimaryLocationCode"),
                new(MetadataExtractor.Formats.Iptc.IptcDirectory.TagCountryOrPrimaryLocationName, "CountryOrPrimaryLocationName"),
                new(MetadataExtractor.Formats.Iptc.IptcDirectory.TagOriginalTransmissionReference, "OriginalTransmissionReference", false),
                new(MetadataExtractor.Formats.Iptc.IptcDirectory.TagHeadline, "Headline", false),
                new(MetadataExtractor.Formats.Iptc.IptcDirectory.TagCredit, "Credit", false),
                new(MetadataExtractor.Formats.Iptc.IptcDirectory.TagSource, "Source", false),
                new(MetadataExtractor.Formats.Iptc.IptcDirectory.TagCopyrightNotice, "CopyrightNotice", false),
                new(MetadataExtractor.Formats.Iptc.IptcDirectory.TagContact, "Contact", false),
                new(MetadataExtractor.Formats.Iptc.IptcDirectory.TagCaption, "Caption", false),
                new(MetadataExtractor.Formats.Iptc.IptcDirectory.TagLocalCaption, "LocalCaption", false),
                new(MetadataExtractor.Formats.Iptc.IptcDirectory.TagCaptionWriter, "CaptionWriter", false),
                new(MetadataExtractor.Formats.Iptc.IptcDirectory.TagRasterizedCaption, "RasterizedCaption", false),
                new(MetadataExtractor.Formats.Iptc.IptcDirectory.TagImageType, "ImageType", false),
                new(MetadataExtractor.Formats.Iptc.IptcDirectory.TagImageOrientation, "ImageOrientation", false),
                new(MetadataExtractor.Formats.Iptc.IptcDirectory.TagLanguageIdentifier, "LanguageIdentifier", false),
                new(MetadataExtractor.Formats.Iptc.IptcDirectory.TagAudioType, "AudioType", false),
                new(MetadataExtractor.Formats.Iptc.IptcDirectory.TagAudioSamplingRate, "AudioSamplingRate", false),
                new(MetadataExtractor.Formats.Iptc.IptcDirectory.TagAudioSamplingResolution, "AudioSamplingResolution", false),
                new(MetadataExtractor.Formats.Iptc.IptcDirectory.TagAudioDuration, "AudioDuration", false),
                new(MetadataExtractor.Formats.Iptc.IptcDirectory.TagAudioOutcue, "AudioOutcue", false),
                new(MetadataExtractor.Formats.Iptc.IptcDirectory.TagJobId, "JobId", false),
                new(MetadataExtractor.Formats.Iptc.IptcDirectory.TagMasterDocumentId, "MasterDocumentId", false),
                new(MetadataExtractor.Formats.Iptc.IptcDirectory.TagShortDocumentId, "ShortDocumentId", false),
                new(MetadataExtractor.Formats.Iptc.IptcDirectory.TagUniqueDocumentId, "UniqueDocumentId", false),
                new(MetadataExtractor.Formats.Iptc.IptcDirectory.TagOwnerId, "OwnerId", false),
                new(MetadataExtractor.Formats.Iptc.IptcDirectory.TagObjectPreviewFileFormat, "ObjectPreviewFileFormat", false),
                new(MetadataExtractor.Formats.Iptc.IptcDirectory.TagObjectPreviewFileFormatVersion, "ObjectPreviewFileFormatVersion", false),
                new(MetadataExtractor.Formats.Iptc.IptcDirectory.TagObjectPreviewData, "ObjectPreviewData", false)
            });

    public static StandardMappings ExifMakernotes_Nikon1 =
        new(
            "EXIF-MakerNotes-Nikon1",
            new[]
            {
                "NikonType1MakernoteDirectory",
            },
            new StandardMapping[]
            {
                new(MetadataExtractor.Formats.Exif.Makernotes.NikonType1MakernoteDirectory.TagCcdSensitivity, "CcdSensitivity", false),
                new(MetadataExtractor.Formats.Exif.Makernotes.NikonType1MakernoteDirectory.TagColorMode, "ColorMode", false),
                new(MetadataExtractor.Formats.Exif.Makernotes.NikonType1MakernoteDirectory.TagDigitalZoom, "DigitalZoom", false),
                new(MetadataExtractor.Formats.Exif.Makernotes.NikonType1MakernoteDirectory.TagConverter, "Converter", false),
                new(MetadataExtractor.Formats.Exif.Makernotes.NikonType1MakernoteDirectory.TagFocus, "Focus", false),
                new(MetadataExtractor.Formats.Exif.Makernotes.NikonType1MakernoteDirectory.TagImageAdjustment, "ImageAdjustment", false),
                new(MetadataExtractor.Formats.Exif.Makernotes.NikonType1MakernoteDirectory.TagQuality, "Quality", false),
                new(MetadataExtractor.Formats.Exif.Makernotes.NikonType1MakernoteDirectory.TagUnknown1, "Unknown1", false),
                new(MetadataExtractor.Formats.Exif.Makernotes.NikonType1MakernoteDirectory.TagUnknown2, "Unknown2", false),
                new(MetadataExtractor.Formats.Exif.Makernotes.NikonType1MakernoteDirectory.TagUnknown3, "Unknown3", false),
                new(MetadataExtractor.Formats.Exif.Makernotes.NikonType1MakernoteDirectory.TagWhiteBalance, "WhiteBalance", false)
            });

    public static StandardMappings ExifMakernotes_Nikon2 =
        new(
            "EXIF-MakerNotes-Nikon2",
            new[]
            {
                "NikonType2MakernoteDirectory",
            },
            new StandardMapping[]
            {
                new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagFirmwareVersion, "FirmwareVersion", false),
                new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagIso1, "Iso1", false),
                new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagQualityAndFileFormat, "QualityAndFileFormat", false),
                new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagCameraWhiteBalance, "CameraWhiteBalance", false),
                new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagCameraSharpening, "CameraSharpening", false),
                new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagAfType, "AfType", false),
                new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagCameraWhiteBalanceFine, "CameraWhiteBalanceFine", false),
                new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagCameraWhiteBalanceRbCoeff, "CameraWhiteBalanceRbCoeff", false),
                new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagIsoRequested, "IsoRequested", false),
                new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagIsoMode, "IsoMode", false),
                new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagDataDump, "DataDump", false),
                new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagProgramShift, "ProgramShift", false),
                new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagExposureDifference, "ExposureDifference", false),
                new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagPreviewIfd, "PreviewIfd", false),
                new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagLensType, "LensType"),
                new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagFlashUsed, "FlashUsed"),
                new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagAfFocusPosition, "AfFocusPosition"),
                new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagShootingMode, "ShootingMode"),
                new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagLensStops, "LensStops"),
                new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagContrastCurve, "ContrastCurve", false),
                new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagLightSource, "LightSource", false),
                new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagShotInfo, "ShotInfo", false),
                new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagColorBalance, "ColorBalance", false),
                new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagLensData, "LensData", false),
                new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagNefThumbnailSize, "NefThumbnailSize", false),
                new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagSensorPixelSize, "SensorPixelSize", false),
                new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagUnknown10, "Unknown10", false),
                new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagSceneAssist, "SceneAssist", false),
                new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagDateStampMode, "DateStampMode", false),
                new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagRetouchHistory, "RetouchHistory", false),
                new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagUnknown12, "Unknown12", false),
                new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagFlashSyncMode, "FlashSyncMode", false),
                new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagAutoFlashMode, "AutoFlashMode", false),
                new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagAutoFlashCompensation, "AutoFlashCompensation", false),
                new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagExposureSequenceNumber, "ExposureSequenceNumber", false),
                new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagColorMode, "ColorMode", false),
                new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagUnknown20, "Unknown20", false),
                new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagImageBoundary, "ImageBoundary", false),
                new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagFlashExposureCompensation, "FlashExposureCompensation", false),
                new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagFlashBracketCompensation, "FlashBracketCompensation", false),
                new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagAeBracketCompensation, "AeBracketCompensation", false),
                new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagFlashMode, "FlashMode", false),
                new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagCropHighSpeed, "CropHighSpeed", false),
                new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagExposureTuning, "ExposureTuning", false),
                new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagCameraSerialNumber, "CameraSerialNumber", false),
                new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagColorSpace, "ColorSpace", false),
                new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagVrInfo, "VrInfo", false),
                new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagImageAuthentication, "ImageAuthentication", false),
                new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagFaceDetect, "FaceDetect", false),
                new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagActiveDLighting, "ActiveDLighting", false),
                new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagPictureControl, "PictureControl", false),
                new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagWorldTime, "WorldTime", false),
                new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagIsoInfo, "IsoInfo", false),
                new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagUnknown36, "Unknown36", false),
                new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagUnknown37, "Unknown37", false),
                new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagUnknown38, "Unknown38", false),
                new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagUnknown39, "Unknown39", false),
                new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagVignetteControl, "VignetteControl", false),
                new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagDistortInfo, "DistortInfo", false),
                new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagUnknown41, "Unknown41", false),
                new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagUnknown42, "Unknown42", false),
                new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagUnknown43, "Unknown43", false),
                new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagUnknown44, "Unknown44", false),
                new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagUnknown45, "Unknown45", false),
                new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagUnknown46, "Unknown46", false),
                new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagUnknown47, "Unknown47", false),
                new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagSceneMode, "SceneMode", false),
                new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagCameraSerialNumber2, "CameraSerialNumber2", false),
                new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagImageDataSize, "ImageDataSize", false),
                new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagUnknown27, "Unknown27", false),
                new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagUnknown28, "Unknown28", false),
                new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagImageCount, "ImageCount", false),
                new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagDeletedImageCount, "DeletedImageCount", false),
                new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagSaturation2, "Saturation2", false),
                new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagDigitalVariProgram, "DigitalVariProgram", false),
                new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagImageStabilisation, "ImageStabilisation", false),
                new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagAfResponse, "AfResponse", false),
                new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagUnknown29, "Unknown29", false),
                new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagUnknown30, "Unknown30", false),
                new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagMultiExposure, "MultiExposure", false),
                new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagHighIsoNoiseReduction, "HighIsoNoiseReduction", false),
                new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagUnknown31, "Unknown31", false),
                new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagToningEffect, "ToningEffect", false),
                new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagUnknown33, "Unknown33", false),
                new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagUnknown48, "Unknown48", false),
                new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagPowerUpTime, "PowerUpTime", false),
                new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagAfInfo2, "AfInfo2", false),
                new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagFileInfo, "FileInfo", false),
                new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagAfTune, "AfTune", false),
                new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagFlashInfo, "FlashInfo", false),
                new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagImageOptimisation, "ImageOptimisation", false),
                new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagImageAdjustment, "ImageAdjustment", false),
                new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagCameraToneCompensation, "CameraToneCompensation", false),
                new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagAdapter, "Adapter", false),
                new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagLens, "Lens"),
                new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagManualFocusDistance, "ManualFocusDistance"),
                new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagDigitalZoom, "DigitalZoom"),
                new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagCameraColorMode, "CameraColorMode", false),
                new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagCameraHueAdjustment, "CameraHueAdjustment", false),
                new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagNefCompression, "NefCompression", false),
                new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagSaturation, "Saturation", false),
                new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagNoiseReduction, "NoiseReduction", false),
                new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagLinearizationTable, "LinearizationTable", false),
                new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagNikonCaptureData, "NikonCaptureData", false),
                new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagRetouchInfo, "RetouchInfo", false),
                new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagPictureControl2, "PictureControl2", false),
                new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagUnknown51, "Unknown51", false),
                new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagPrintImageMatchingInfo, "PrintImageMatchingInfo", false),
                new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagUnknown52, "Unknown52", false),
                new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagUnknown53, "Unknown53", false),
                new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagNikonCaptureVersion, "NikonCaptureVersion", false),
                new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagNikonCaptureOffsets, "NikonCaptureOffsets", false),
                new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagNikonScan, "NikonScan", false),
                new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagUnknown54, "Unknown54", false),
                new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagNefBitDepth, "NefBitDepth", false),
                new(MetadataExtractor.Formats.Exif.Makernotes.NikonType2MakernoteDirectory.TagUnknown55, "Unknown55", false),
            });

    //    public static StandardMappings Fmt =
    //        new(
    //            "FMT",
    //            new StandardMapping[]
    //            {
    //                new(MetadataExtractor.Formats., ""),
    //            });

    public static StandardMappings Exif =
        new(
            "EXIF",
            new[]
            {
                "ExifIFD0Directory",
                "ExifInteropDirectory",
                "ExifSubIFDDirectory"
            },
            new StandardMapping[]
            {
                new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagAperture, "Aperture"),
                new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagInteropIndex, "InteropIndex", false),
                new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagInteropVersion, "InteropVersion", false),
                new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagNewSubfileType, "NewSubfileType", false),
                new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagSubfileType, "SubfileType", false),
                new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagImageWidth, "ImageWidth"),
                new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagImageHeight, "ImageHeight"),
                new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagBitsPerSample, "BitsPerSample", false),
                new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagCompression, "Compression", false),
                new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagPhotometricInterpretation, "PhotometricInterpretation", false),
                new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagThresholding, "Thresholding", false),
                new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagFillOrder, "FillOrder", false),
                new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagDocumentName, "DocumentName", false),
                new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagImageDescription, "ImageDescription"),
                new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagMake, "Make"),
                new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagModel, "Model"),
                new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagStripOffsets, "StripOffsets", false),
                new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagOrientation, "Orientation", false),
                new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagSamplesPerPixel, "SamplesPerPixel", false),
                new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagRowsPerStrip, "RowsPerStrip", false),
                new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagStripByteCounts, "StripByteCounts", false),
                new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagMinSampleValue, "MinSampleValue", false),
                new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagMaxSampleValue, "MaxSampleValue", false),
                new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagXResolution, "XResolution"),
                new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagYResolution, "YResolution"),
                new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagPlanarConfiguration, "PlanarConfiguration", false),
                new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagPageName, "PageName", false),
                new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagResolutionUnit, "ResolutionUnit", false),
                new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagPageNumber, "PageNumber", false),
                new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagTransferFunction, "TransferFunction", false),
                new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagSoftware, "Software", false),
                new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagDateTime, "DateTime", false),
                new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagArtist, "Artist", false),
                new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagPredictor, "Predictor", false),
                new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagHostComputer, "HostComputer", false),
                new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagWhitePoint, "WhitePoint", false),
                new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagPrimaryChromaticities, "PrimaryChromaticities", false),
                new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagTileWidth, "TileWidth", false),
                new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagTileLength, "TileLength", false),
                new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagTileOffsets, "TileOffsets", false),
                new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagTileByteCounts, "TileByteCounts", false),
                new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagSubIfdOffset, "SubIfdOffset", false),
                new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagExtraSamples, "ExtraSamples", false),
                new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagSampleFormat, "SampleFormat", false),
                new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagTransferRange, "TransferRange", false),
                new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagJpegTables, "JpegTables", false),
                new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagJpegProc, "JpegProc", false),
                new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagJpegLosslessPredictors, "JpegLosslessPredictors", false),
                new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagJpegPointTransforms, "JpegPointTransforms", false),
                new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagJpegQTables, "JpegQTables", false),
                new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagJpegDcTables, "JpegDcTables", false),
                new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagJpegAcTables, "JpegAcTables", false),
                new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagYCbCrSubsampling, "YCbCrSubsampling", false),
                new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagYCbCrPositioning, "YCbCrPositioning", false),
                new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagReferenceBlackWhite, "ReferenceBlackWhite", false),
                new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagStripRowCounts, "StripRowCounts", false),
                new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagApplicationNotes, "ApplicationNotes", false),
                new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagRelatedImageFileFormat, "RelatedImageFileFormat", false),
                new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagRelatedImageWidth, "RelatedImageWidth", false),
                new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagRelatedImageHeight, "RelatedImageHeight", false),
                new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagRating, "Rating", false),
                new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagRatingPercent, "RatingPercent", false),
                new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagCfaRepeatPatternDim, "CfaRepeatPatternDim", false),
                new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagCfaPattern2, "CfaPattern2", false),
                new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagBatteryLevel, "BatteryLevel", false),
                new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagCopyright, "Copyright", false),
                new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagExposureTime, "ExposureTime"),
                new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagFNumber, "FNumber"),
                new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagPixelScale, "PixelScale", false),
                new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagIptcNaa, "IptcNaa", false),
                new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagModelTiePoint, "ModelTiePoint", false),
                new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagPhotoshopSettings, "PhotoshopSettings", false),
                new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagInterColorProfile, "InterColorProfile", false),
                new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagExposureProgram, "ExposureProgram"),
                new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagSpectralSensitivity, "SpectralSensitivity", false),
                new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagIsoEquivalent, "IsoEquivalent", false),
                new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagOptoElectricConversionFunction, "OptoElectricConversionFunction", false),
                new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagInterlace, "Interlace", false),
                new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagTimeZoneOffset, "TimeZoneOffset", false),
                new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagSelfTimerMode, "SelfTimerMode", false),
                new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagSensitivityType, "SensitivityType", false),
                new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagStandardOutputSensitivity, "StandardOutputSensitivity", false),
                new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagRecommendedExposureIndex, "RecommendedExposureIndex", false),
                new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagIsoSpeed, "IsoSpeed"),
                new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagIsoSpeedLatitudeYYY, "IsoSpeedLatitudeYYY", false),
                new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagIsoSpeedLatitudeZZZ, "IsoSpeedLatitudeZZZ", false),
                new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagExifVersion, "ExifVersion", false),
                new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagDateTimeOriginal, "DateTimeOriginal", false),
                new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagDateTimeDigitized, "DateTimeDigitized", false),
                new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagTimeZone, "TimeZone", false),
                new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagTimeZoneOriginal, "TimeZoneOriginal", false),
                new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagTimeZoneDigitized, "TimeZoneDigitized", false),
                new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagComponentsConfiguration, "ComponentsConfiguration", false),
                new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagCompressedAverageBitsPerPixel, "CompressedAverageBitsPerPixel", false),
                new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagShutterSpeed, "ShutterSpeed", false),
                new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagAperture, "Aperture"),
                new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagBrightnessValue, "BrightnessValue", false),
                new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagExposureBias, "ExposureBias"),
                new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagMaxAperture, "MaxAperture", false),
                new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagSubjectDistance, "SubjectDistance", false),
                new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagMeteringMode, "MeteringMode", false),
                new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagWhiteBalance, "WhiteBalance", false),
                new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagFlash, "Flash"),
                new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagFocalLength, "FocalLength"),
                new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagFlashEnergyTiffEp, "FlashEnergyTiffEp", false),
                new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagSpatialFreqResponseTiffEp, "SpatialFreqResponseTiffEp", false),
                new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagNoise, "Noise", false),
                new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagFocalPlaneXResolutionTiffEp, "FocalPlaneXResolutionTiffEp", false),
                new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagFocalPlaneYResolutionTiffEp, "FocalPlaneYResolutionTiffEp", false),
                new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagImageNumber, "ImageNumber", false),
                new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagSecurityClassification, "SecurityClassification", false),
                new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagImageHistory, "ImageHistory", false),
                new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagSubjectLocationTiffEp, "SubjectLocationTiffEp", false),
                new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagExposureIndexTiffEp, "ExposureIndexTiffEp", false),
                new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagStandardIdTiffEp, "StandardIdTiffEp", false),
                new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagMakernote, "Makernote", false),
                new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagUserComment, "UserComment"),
                new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagSubsecondTime, "SubsecondTime", false),
                new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagSubsecondTimeOriginal, "SubsecondTimeOriginal", false),
                new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagSubsecondTimeDigitized, "SubsecondTimeDigitized", false),
                new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagWinTitle, "WinTitle", false),
                new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagWinComment, "WinComment", false),
                new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagWinAuthor, "WinAuthor", false),
                new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagWinKeywords, "WinKeywords", false),
                new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagWinSubject, "WinSubject", false),
                new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagFlashpixVersion, "FlashpixVersion", false),
                new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagColorSpace, "ColorSpace", false),
                new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagExifImageWidth, "ExifImageWidth", false),
                new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagExifImageHeight, "ExifImageHeight", false),
                new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagRelatedSoundFile, "RelatedSoundFile", false),
                new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagFlashEnergy, "FlashEnergy", false),
                new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagSpatialFreqResponse, "SpatialFreqResponse", false),
                new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagFocalPlaneXResolution, "FocalPlaneXResolution", false),
                new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagFocalPlaneYResolution, "FocalPlaneYResolution", false),
                new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagFocalPlaneResolutionUnit, "FocalPlaneResolutionUnit", false),
                new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagSubjectLocation, "SubjectLocation", false),
                new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagExposureIndex, "ExposureIndex", false),
                new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagSensingMethod, "SensingMethod", false),
                new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagFileSource, "FileSource", false),
                new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagSceneType, "SceneType", false),
                new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagCfaPattern, "CfaPattern", false),
                new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagCustomRendered, "CustomRendered", false),
                new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagExposureMode, "ExposureMode", false),
                new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagWhiteBalanceMode, "WhiteBalanceMode", false),
                new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagDigitalZoomRatio, "DigitalZoomRatio", false),
                new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.Tag35MMFilmEquivFocalLength, "35MMFilmEquivFocalLength"),
                new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagSceneCaptureType, "SceneCaptureType", false),
                new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagGainControl, "GainControl", false),
                new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagContrast, "Contrast", false),
                new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagSaturation, "Saturation", false),
                new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagSharpness, "Sharpness", false),
                new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagDeviceSettingDescription, "DeviceSettingDescription", false),
                new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagSubjectDistanceRange, "SubjectDistanceRange", false),
                new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagImageUniqueId, "ImageUniqueId", false),
                new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagCameraOwnerName, "CameraOwnerName", false),
                new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagBodySerialNumber, "BodySerialNumber", false),
                new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagLensSpecification, "LensSpecification"),
                new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagLensMake, "LensMake"),
                new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagLensModel, "LensModel"),
                new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagLensSerialNumber, "LensSerialNumber"),
                new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagGdalMetadata, "GdalMetadata", false),
                new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagGdalNoData, "GdalNoData", false),
                new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagGamma, "Gamma", false),
                new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagPrintImageMatchingInfo, "PrintImageMatchingInfo", false),
                new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagPanasonicTitle, "PanasonicTitle", false),
                new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagPanasonicTitle2, "PanasonicTitle2", false),
                new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagPadding, "Padding", false),
                new(MetadataExtractor.Formats.Exif.ExifDirectoryBase.TagLens, "Lens"),
            });

    public static StandardMappings ExifGps =
        new(
            "EXIF",
            new[]
            {
                "GpsDirectory",
            },
            new StandardMapping[]
            {
                new(MetadataExtractor.Formats.Exif.GpsDirectory.TagVersionId, "VersionId", false),
                new(MetadataExtractor.Formats.Exif.GpsDirectory.TagLatitudeRef, "LatitudeRef"),
                new(MetadataExtractor.Formats.Exif.GpsDirectory.TagLatitude, "Latitude"),
                new(MetadataExtractor.Formats.Exif.GpsDirectory.TagLongitudeRef, "LongitudeRef"),
                new(MetadataExtractor.Formats.Exif.GpsDirectory.TagLongitude, "Longitude"),
                new(MetadataExtractor.Formats.Exif.GpsDirectory.TagAltitudeRef, "AltitudeRef"),
                new(MetadataExtractor.Formats.Exif.GpsDirectory.TagAltitude, "Altitude"),
                new(MetadataExtractor.Formats.Exif.GpsDirectory.TagTimeStamp, "TimeStamp", false),
                new(MetadataExtractor.Formats.Exif.GpsDirectory.TagSatellites, "Satellites", false),
                new(MetadataExtractor.Formats.Exif.GpsDirectory.TagStatus, "Status", false),
                new(MetadataExtractor.Formats.Exif.GpsDirectory.TagMeasureMode, "MeasureMode", false),
                new(MetadataExtractor.Formats.Exif.GpsDirectory.TagDop, "Dop", false),
                new(MetadataExtractor.Formats.Exif.GpsDirectory.TagSpeedRef, "SpeedRef", false),
                new(MetadataExtractor.Formats.Exif.GpsDirectory.TagSpeed, "Speed", false),
                new(MetadataExtractor.Formats.Exif.GpsDirectory.TagTrackRef, "TrackRef", false),
                new(MetadataExtractor.Formats.Exif.GpsDirectory.TagTrack, "Track", false),
                new(MetadataExtractor.Formats.Exif.GpsDirectory.TagImgDirectionRef, "ImgDirectionRef", false),
                new(MetadataExtractor.Formats.Exif.GpsDirectory.TagImgDirection, "ImgDirection", false),
                new(MetadataExtractor.Formats.Exif.GpsDirectory.TagMapDatum, "MapDatum", false),
                new(MetadataExtractor.Formats.Exif.GpsDirectory.TagDestLatitudeRef, "DestLatitudeRef", false),
                new(MetadataExtractor.Formats.Exif.GpsDirectory.TagDestLatitude, "DestLatitude", false),
                new(MetadataExtractor.Formats.Exif.GpsDirectory.TagDestLongitudeRef, "DestLongitudeRef", false),
                new(MetadataExtractor.Formats.Exif.GpsDirectory.TagDestLongitude, "DestLongitude", false),
                new(MetadataExtractor.Formats.Exif.GpsDirectory.TagDestBearingRef, "DestBearingRef", false),
                new(MetadataExtractor.Formats.Exif.GpsDirectory.TagDestBearing, "DestBearing", false),
                new(MetadataExtractor.Formats.Exif.GpsDirectory.TagDestDistanceRef, "DestDistanceRef", false),
                new(MetadataExtractor.Formats.Exif.GpsDirectory.TagDestDistance, "DestDistance", false),
                new(MetadataExtractor.Formats.Exif.GpsDirectory.TagProcessingMethod, "ProcessingMethod", false),
                new(MetadataExtractor.Formats.Exif.GpsDirectory.TagAreaInformation, "AreaInformation", false),
                new(MetadataExtractor.Formats.Exif.GpsDirectory.TagDateStamp, "DateStamp", false),
                new(MetadataExtractor.Formats.Exif.GpsDirectory.TagDifferential, "Differential", false),
                new(MetadataExtractor.Formats.Exif.GpsDirectory.TagHPositioningError, "HPositioningError", false)
            });

    public static StandardMappings[] KnownMappings =
    {
        Jpeg,
        Jfif,
        Iptc,
        ExifMakernotes_Nikon1,
        ExifMakernotes_Nikon2,
        Exif,
        Tcat
    };

    /*----------------------------------------------------------------------------
        %%Function: GetStandardsMappingFromType
        %%Qualified: Thetacat.Standards.StandardsMappings.GetStandardsMappingFromType

    ----------------------------------------------------------------------------*/
    public static StandardMappings? GetStandardsMappingFromType(string typeName)
    {
        foreach (StandardMappings standard in KnownMappings)
        {
            foreach (string standardTypeName in standard.TypeNames)
            {
                if (string.Compare(standardTypeName, typeName, StringComparison.InvariantCultureIgnoreCase) == 0)
                    return standard;
            }
        }

        return null;
    }
}
