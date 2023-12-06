using System.Collections.Generic;
using System.Collections.ObjectModel;
using Thetacat.Standards;

namespace Thetacat.Migration.Elements.Metadata.UI;

public class PseMetadataSchema
{
    public ObservableCollection<PseMetadata> MetadataItems { get; init; }

    public PseMetadataSchema(List<PseMetadata> metadataItems)
    {
        MetadataItems = new ObservableCollection<PseMetadata>(metadataItems);
    }

    void FillMetadataFromSchemaMapping<T>(PseMetadata metadata, SchemaMapping<T> pseMapping)
    {
        if (pseMapping.StandardId == MetatagStandards.Standard.User)
        {
            metadata.Standard = "user";
            metadata.Tag = pseMapping.Name;
            metadata.Description = pseMapping.Description;
            metadata.Migrate = true;
        }
        else if (pseMapping.StandardId == MetatagStandards.Standard.Unknown)
        {
            metadata.Standard = "standard";
            metadata.Tag = string.Empty;
            metadata.Description = string.Empty;
            metadata.Migrate = true;
        }
        else if (pseMapping.StandardId != MetatagStandards.Standard.Unknown)
        {
            StandardDefinitions definition = MetatagStandards.GetStandardMappings(pseMapping.StandardId);

            metadata.Standard = definition.Tag;
            metadata.Tag = definition.Properties[pseMapping.ItemTag].TagName;
            metadata.Description = $"{definition.Tag} {metadata.Tag}";
            metadata.Migrate = true;
        }
    }

    public void PopulateBuiltinMappings()
    {
        foreach (PseMetadata metadata in MetadataItems)
        {
            switch (metadata.PseDatatype)
            {
                case "integer_type":
                {
                    if (SchemaMappings.IntMappings.TryGetValue(metadata.PseIdentifier, out SchemaMapping<int>? pseMapping))
                        FillMetadataFromSchemaMapping(metadata, pseMapping);

                    break;
                }
                case "string_type":
                {
                    if (SchemaMappings.StringMappings.TryGetValue(metadata.PseIdentifier, out SchemaMapping<string>? pseMapping))
                        FillMetadataFromSchemaMapping(metadata, pseMapping);

                    break;
                }
                case "decimal_type":
                {
                    if (SchemaMappings.DecimalMappings.TryGetValue(metadata.PseIdentifier, out SchemaMapping<double>? pseMapping))
                        FillMetadataFromSchemaMapping(metadata, pseMapping);

                    break;
                }
            }
        }
    }
}
