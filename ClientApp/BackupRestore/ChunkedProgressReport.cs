using System.Collections.Generic;
using System.Windows.Documents;
using Thetacat.UI;

namespace Thetacat.Export;

class ProgressChunkItem
{
    public string Name { get; set; } = string.Empty;
    public double Weight { get; set; } = 0;
}

public class ChunkedProgressReport
{
    private double m_blockStart = 0.0;
    private double m_blockEnd = 0.0;

    List<ProgressChunkItem> m_chunks = new();
    private IProgressReport m_progress;

    public ChunkedProgressReport(IProgressReport progress)
    {
        m_progress = progress;
    }

    public void AddWeightedChunk(string name, double weight)
    {
        m_chunks.Add(new ProgressChunkItem() { Name = name, Weight = weight });
    }

    public double GetChunkPercent(string name)
    {
        double total = 0;
        double cumulative = 0;
        bool found = false;

        foreach (var chunk in m_chunks)
        {
            total += chunk.Weight;
            if (!found)
                cumulative += chunk.Weight;

            if (chunk.Name == name)
                found = true;
        }

        if (!found)
            throw new System.ArgumentException("Chunk not found", nameof(name));

        return (cumulative / total) * 100.0;
    }

    /*----------------------------------------------------------------------------
        %%Function: StartNextBlock
        %%Qualified: Thetacat.Export.BackupDatabase.StartNextBlock
    ----------------------------------------------------------------------------*/
    public void StartNextBlock(double pctEnd)
    {
        m_progress!.UpdateProgress(m_blockEnd);
        m_blockStart = m_blockEnd;
        m_blockEnd = pctEnd;
    }

    public void StartBlock(string name)
    {
        double pctEnd = GetChunkPercent(name);
        StartNextBlock(pctEnd);
    }

    /*----------------------------------------------------------------------------
        %%Function: UpdateProgress
        %%Qualified: Thetacat.Export.BackupDatabase.UpdateProgress
    ----------------------------------------------------------------------------*/
    public void UpdateProgress(int idxCur, int idxMax)
    {
        m_progress!.UpdateProgress(m_blockStart + ((idxCur * 100.0) / idxMax) * ((m_blockEnd - m_blockStart) / m_blockEnd));
    }

    public void WorkCompleted()
    {
        m_progress!.UpdateProgress(m_blockEnd);
        m_progress!.WorkCompleted();
    }
}
