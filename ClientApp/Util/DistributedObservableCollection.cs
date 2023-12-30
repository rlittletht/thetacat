using System.Collections.ObjectModel;
using System.Security.RightsManagement;
using Thetacat.UI;

namespace Thetacat.Util;

public interface IObservableCollectionHolder<T>
{
    public ObservableCollection<T> Items { get; set; }
}

// this is an observable collection or observable collections
public class DistributedObservableCollection<T, T1> 
    where T : class, IObservableCollectionHolder<T1>
{
    public delegate T LineFactoryDelegate(T? reference);
    public delegate void MoveLinePropertiesDelegate(T from, T to);

    private readonly ObservableCollection<T> m_collection = new();

    public ObservableCollection<T> TopCollection => m_collection;

    private int m_itemsPerLine = 0;
    private readonly LineFactoryDelegate m_lineFactory;
    private readonly MoveLinePropertiesDelegate m_moveLineProperties;

    public DistributedObservableCollection(LineFactoryDelegate lineFactory, MoveLinePropertiesDelegate moveLinePropertiesDelegate)
    {
        m_lineFactory = lineFactory;
        m_moveLineProperties = moveLinePropertiesDelegate;
    }

    public void UpdateItemsPerLine(int newItemsPerLine)
    {
        m_itemsPerLine = newItemsPerLine;
    }

    public void AddItem(T1 itemToAdd)
    {
        // figure out where to add this item
        if (m_collection.Count == 0
            || m_collection[m_collection.Count - 1].Items.Count == m_itemsPerLine)
        {
            T newLine = m_lineFactory(null);
            
            m_collection.Add(newLine);
        }
        
        m_collection[m_collection.Count - 1].Items.Add(itemToAdd);
    }
}
