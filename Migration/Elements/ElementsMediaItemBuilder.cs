using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace Thetacat.Migration.Elements;

class ElementsMediaItemBuilder
{
    private MediaItem m_building = new MediaItem();

    public static ElementsMediaItemBuilder Create() => new ();

    public ElementsMediaItemBuilder SetID(string id)
    {
        m_building.ID = id;
        return this;
    }

    public ElementsMediaItemBuilder SetFilename(string name)
    {
        m_building.Filename = name;
        return this;
    }

    public ElementsMediaItemBuilder SetFullPath(string path)
    {
        m_building.FullPath = path;
        return this;
    }

    public ElementsMediaItemBuilder SetFilePath(string path)
    {
        m_building.FilePathSearch = path;
        return this;
    }

    public ElementsMediaItemBuilder SetMimeType(string mimeType)
    {
        m_building.MimeType = mimeType;
        return this;
    }

    public ElementsMediaItemBuilder SetVolumeId(string volumeId)
    {
        m_building.VolumeId = volumeId;
        return this;
    }

    public ElementsMediaItemBuilder SetVolumeName(string volumenName)
    {
        m_building.VolumeName = volumenName;
        return this;
    }

    public MediaItem Build()
    {
        return m_building;
    }
}