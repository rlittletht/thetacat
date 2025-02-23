using System.Collections.Generic;
using System.Windows.Documents;

namespace Thetacat.Export;

class ProgressChunkItem
{
    public string Name { get; set; } = string.Empty;
    public double Weight { get; set; } = 0;
}

public class ProgressChunks
{
    List<ProgressChunkItem> m_chunks = new();

    public void AddWeightedChunk(string name, double weight)
    {
        m_chunks.Add(new ProgressChunkItem(){ Name = name, Weight = weight });
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

        return (total / cumulative * 100.0);
    }
}
