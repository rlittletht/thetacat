namespace Thetacat.Migration.Elements.Media;

class PseMediaItemBuilder
{
    private PseMediaItem m_building = new PseMediaItem();

    public static PseMediaItemBuilder Create() => new();

    public PseMediaItemBuilder SetID(int id)
    {
        m_building.ID = id;
        return this;
    }

    public PseMediaItemBuilder SetFilename(string name)
    {
        m_building.Filename = name;
        return this;
    }

    public PseMediaItemBuilder SetFullPath(string path)
    {
        m_building.FullPath = path;
        return this;
    }

    public PseMediaItemBuilder SetFilePath(string path)
    {
        m_building.FilePathSearch = path;
        return this;
    }

    public PseMediaItemBuilder SetMimeType(string mimeType)
    {
        m_building.MimeType = mimeType;
        return this;
    }

    public PseMediaItemBuilder SetVolumeId(string volumeId)
    {
        m_building.VolumeId = volumeId;
        return this;
    }

    public PseMediaItemBuilder SetVolumeName(string volumenName)
    {
        m_building.VolumeName = volumenName;
        return this;
    }

    public PseMediaItem Build()
    {
        return m_building;
    }
}