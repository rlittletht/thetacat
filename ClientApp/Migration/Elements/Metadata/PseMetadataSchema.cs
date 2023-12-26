using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Thetacat.Migration.Elements.Media;
using Thetacat.Standards;
using Thetacat.TCore.TcSqlLite;
using Thetacat.Types;

namespace Thetacat.Migration.Elements.Metadata.UI;

public class PseMetadataSchema
{
    public ObservableCollection<PseMetadata> MetadataItems { get; init; }

    public PseMetadataSchema(IEnumerable<PseMetadata> metadataItems)
    {
        MetadataItems = new ObservableCollection<PseMetadata>(metadataItems);
    }

    void FillMetadataFromSchemaMapping<T>(PseMetadata metadata, SchemaMapping<T> pseMapping)
    {
        if (pseMapping.StandardId == MetatagStandards.Standard.User)
        {
            metadata.StandardTag = "user";
            metadata.PropertyTag = pseMapping.Name;
            metadata.Description = pseMapping.Description;
            metadata.Checked = true;
        }
        else if (pseMapping.StandardId == MetatagStandards.Standard.Unknown)
        {
            metadata.StandardTag = "builtin";
            metadata.PropertyTag = string.Empty;
            metadata.Description = string.Empty;
            metadata.Checked = false; // these are builtin so they don't need a metatag definition
        }
        else if (pseMapping.StandardId != MetatagStandards.Standard.Unknown)
        {
            StandardDefinitions definition = MetatagStandards.GetStandardMappings(pseMapping.StandardId);

            metadata.StandardTag = definition.StandardTag;
            metadata.PropertyTag = definition.Properties[pseMapping.ItemTag].PropertyTagName;
            metadata.Description = $"{definition.StandardTag} {metadata.PropertyTag}";
            metadata.Checked = true;
        }
    }

    private Dictionary<string, PseMetadata>? m_lookupTable;

    public PseMetadata LookupPseIdentifier(string identifier)
    {
        if (m_lookupTable == null)
        {
            m_lookupTable = new Dictionary<string, PseMetadata>();

            foreach (PseMetadata item in MetadataItems)
            {
                m_lookupTable.Add(item.PseIdentifier, item);
            }
        }

        if (!m_lookupTable.ContainsKey(identifier))
            throw new CatExceptionInternalFailure($"identifier {identifier} not found in pse metadata schema");

        return m_lookupTable[identifier];
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
                case "date_time_type":
                {
                    if (SchemaMappings.DateTimeMappings.TryGetValue(metadata.PseIdentifier, out SchemaMapping<DateTime>? pseMapping))
                        FillMetadataFromSchemaMapping(metadata, pseMapping);

                    break;
                }

            }
        }
    }

    public void PropagateMetadataToBuiltins(PseMediaItem item)
    {
        foreach (KeyValuePair<string, string> data in item.PseMetadataValues)
        {
            PseMetadata metadata = LookupPseIdentifier(data.Key);
            switch (metadata.PseDatatype)
            {
                case "integer_type":
                {
                    if (SchemaMappings.IntMappings.TryGetValue(metadata.PseIdentifier, out SchemaMapping<int>? pseMapping))
                    {
                        Int32 n = Int32.Parse(data.Value);
                        pseMapping.SetMediaItemBuiltins(item, n);
                    }

                    break;
                }
                case "string_type":
                {
                    if (SchemaMappings.StringMappings.TryGetValue(metadata.PseIdentifier, out SchemaMapping<string>? pseMapping))
                        pseMapping.SetMediaItemBuiltins(item, data.Value);

                    break;
                }
                case "decimal_type":
                {
                    if (SchemaMappings.DecimalMappings.TryGetValue(metadata.PseIdentifier, out SchemaMapping<double>? pseMapping))
                    {
                        double d = double.Parse(data.Value);
                        pseMapping.SetMediaItemBuiltins(item, d);
                    }

                    break;
                }
                case "date_time_type":
                {
                    if (SchemaMappings.DateTimeMappings.TryGetValue(metadata.PseIdentifier, out SchemaMapping<DateTime>? pseMapping))
                    {
                        DateTime time = DateTime.Parse(SQLite.Iso8601DateFromPackedSqliteDate(data.Value));
                        pseMapping.SetMediaItemBuiltins(item, time);
                    }

                    break;
                }
            }
        }
    }
}
