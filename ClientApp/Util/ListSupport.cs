using System.Collections.Generic;

namespace Thetacat.Util;

public class ListSupport
{
    /*----------------------------------------------------------------------------
        %%Function: AddItemToMappedList
        %%Qualified: Thetacat.Util.ListSupport.AddItemToMappedList<T1, T2>

        Add an item to a dictionary that maps a key to a list of items
    ----------------------------------------------------------------------------*/
    public static List<T2> AddItemToMappedList<T1, T2>(Dictionary<T1, List<T2>> map, T1 key, T2 newItem) where T1 : notnull
    {
        if (!map.TryGetValue(key, out List<T2>? list))
        {
            list = new List<T2>();
            map.Add(key, list);
        }

        list.Add(newItem);
        return list;
    }

    public static bool RemoveItemFromMappedList<T1, T2>(Dictionary<T1, List<T2>> map, T1 key, T2 newItem) where T1 : notnull
    {
        if (!map.TryGetValue(key, out List<T2>? list))
            return false;

        list.Remove(newItem);
        return true;
    }

    /*----------------------------------------------------------------------------
        %%Function: AddItemToMappedSortedList
        %%Qualified: Thetacat.Util.ListSupport.AddItemToMappedSortedList<T1, T2, T3>

        Add an item to a dictionary tha maps a key to a sorted list of items
    ----------------------------------------------------------------------------*/
    public static SortedList<T2, T3> AddItemToMappedSortedList<T1, T2, T3>(Dictionary<T1, SortedList<T2, T3>> map, T1 key, T2 itemKey, T3 item)
        where T1 : notnull
        where T2 : notnull
    {
        if (!map.TryGetValue(key, out SortedList<T2, T3>? list))
        {
            list = new SortedList<T2, T3>();
            map.Add(key, list);
        }

        list.Add(itemKey, item);
        return list;
    }

    public static bool RemoveItemFromMappedSortedList<T1, T2, T3>(Dictionary<T1, SortedList<T2, T3>> map, T1 key, T2 itemKey)
        where T1 : notnull
        where T2 : notnull
    {
        if (!map.TryGetValue(key, out SortedList<T2, T3>? list))
            return false;

        list.Remove(itemKey);
        return true;
    }

    /*----------------------------------------------------------------------------
        %%Function: AddItemToMappedMapList
        %%Qualified: Thetacat.Util.ListSupport.AddItemToMappedMapList<T1, T2, T3>

        Add an item to a dictionary that maps a key to dictionary mapping
        to lists.
    ----------------------------------------------------------------------------*/
    public static List<T3> AddItemToMappedMapList<T1, T2, T3>(Dictionary<T1, Dictionary<T2, List<T3>>> map, T1 key, T2 listKey, T3 newItem)
        where T1 : notnull
        where T2 : notnull
    {
        if (!map.TryGetValue(key, out Dictionary<T2, List<T3>>? maps))
        {
            maps = new Dictionary<T2, List<T3>>();
            map.Add(key, maps);
        }

        if (!maps.TryGetValue(listKey, out List<T3>? list))
        {
            list = new List<T3>();
            maps.Add(listKey, list);
        }

        list.Add(newItem);

        return list;
    }

    public static bool RemoveItemFromMappedMapList<T1, T2, T3>(Dictionary<T1, Dictionary<T2, List<T3>>> map, T1 key, T2 listKey, T3 newItem)
        where T1 : notnull
        where T2 : notnull
    {
        if (!map.TryGetValue(key, out Dictionary<T2, List<T3>>? maps))
            return false;

        if (!maps.TryGetValue(listKey, out List<T3>? list))
            return false;

        list.Remove(newItem);
        return true;
    }

}
