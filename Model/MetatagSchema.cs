using System;
using System.Collections.Generic;
using System.Windows.Markup;
using Emgu.CV.Features2D;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Thetacat.ServiceClient;

namespace Thetacat.Model;

public class MetatagSchema
{
    public List<Metatag> Metatags { get; set; } = new List<Metatag>();
    public int SchemaVersion { get; set; } = 0;

    public static MetatagSchema CreateFromService(ServiceMetatagSchema serviceMetatagSchema)
    {
        MetatagSchema schema = new();

        if (serviceMetatagSchema.Metatags != null)
        {
            foreach (ServiceMetatag serviceMetatag in serviceMetatagSchema.Metatags)
            {
                schema.Metatags.Add(Metatag.CreateFromService(serviceMetatag));
            }
        }

        schema.SchemaVersion = serviceMetatagSchema.SchemaVersion ?? 0;
        return schema;
    }
}
