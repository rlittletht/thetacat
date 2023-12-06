using System.Collections.Generic;
using System.Collections.ObjectModel;
using Thetacat.Standards;

namespace Thetacat.Migration.Elements.Metadata;

public class PseMetadataSchema
{
    public ObservableCollection<PseMetadata> MetadataItems { get; init; }

    public PseMetadataSchema(List<PseMetadata> metadataItems)
    {
        MetadataItems = new ObservableCollection<PseMetadata>(metadataItems);
    }

    void FillMetadataFromSchemaMapping<T>(PseMetadata metadata, SchemaMapping<T> pseMapping)
    {
        if (pseMapping.StandardId == MetatagStandards.Builtin.User)
        {
            metadata.Standard = "user";
            metadata.Tag = pseMapping.Name;
            metadata.Description = pseMapping.Description;
        }
        else if (pseMapping.StandardId != MetatagStandards.Builtin.Unknown)
        {
            StandardMappings mapping = MetatagStandards.GetBuiltinStandard(pseMapping.StandardId);

            metadata.Standard = mapping.Tag;
            metadata.Tag = mapping.Properties[pseMapping.ItemTag].TagName;
            metadata.Description = $"{mapping.Tag} {metadata.Tag}";
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
