using System.IO;
using System.Runtime.CompilerServices;
using System.Xml;
using Thetacat.BackupRestore.Restore;
using Thetacat.Metatags.Model;
using Thetacat.UI;
using XMLIO;

namespace Thetacat.Export;

public class RestoreDatabase
{
    private string m_backupSource;
    public FullExportRestore? FullExportRestore;

    public RestoreDatabase(string backupSource)
    {
        m_backupSource = backupSource;
    }

    private IProgressReport? m_progress;

    public bool DoRestore(IProgressReport? progress)
    {
        m_progress = progress;

        using Stream stm = File.Open(m_backupSource, FileMode.Open);
        using XmlReader reader = XmlReader.Create(stm);

        if (!XmlIO.Read(reader))
            return true;

        XmlIO.SkipNonContent(reader);

        FullExportRestore = new FullExportRestore(reader);

        m_progress?.WorkCompleted();
        return true;
    }
}
