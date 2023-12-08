using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace Thetacat.Migration.Elements.Media;

class MediaItemBuilder
{
    private MediaItem m_building = new MediaItem();

    public static MediaItemBuilder Create() => new();

    public MediaItemBuilder SetID(int id)
    {
        m_building.ID = id;
        return this;
    }

    public MediaItemBuilder SetFilename(string name)
    {
        m_building.Filename = name;
        return this;
    }

    public MediaItemBuilder SetFullPath(string path)
    {
        m_building.FullPath = path;
        return this;
    }

    public MediaItemBuilder SetFilePath(string path)
    {
        m_building.FilePathSearch = path;
        return this;
    }

    public MediaItemBuilder SetMimeType(string mimeType)
    {
        m_building.MimeType = mimeType;
        return this;
    }

    public MediaItemBuilder SetVolumeId(string volumeId)
    {
        m_building.VolumeId = volumeId;
        return this;
    }

    public MediaItemBuilder SetVolumeName(string volumenName)
    {
        m_building.VolumeName = volumenName;
        return this;
    }

    public MediaItem Build()
    {
        return m_building;
    }
}