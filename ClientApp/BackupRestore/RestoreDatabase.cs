using System.IO;
using System.Xml;
using Thetacat.BackupRestore.Restore;
using Thetacat.Metatags.Model;
using Thetacat.UI;
using XMLIO;

namespace Thetacat.Export;

public class RestoreDatabase
{
    private string m_backupSource;

    public RestoreDatabase(string backupSource)
    {
        m_backupSource = backupSource;
    }

    private IProgressReport? m_progress;

    public bool DoRestore(IProgressReport? progress)
    {
        m_progress = progress;

        Stream stm = File.Open("c:\\temp\\backup.xml", FileMode.Open);
        XmlReader reader = XmlReader.Create(stm);

        if (!XmlIO.Read(reader))
            return true;

        XmlIO.SkipNonContent(reader);

        FullExportRestore fullExport = new FullExportRestore(reader);

        m_progress?.WorkCompleted();
        return true;
    }
}
