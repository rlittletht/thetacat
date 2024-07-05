namespace Thetacat.Model.Caching;

public enum ScanCacheType
{
    // Predictive scans are limited to the most likely changed media items
    // (media that is part of a stack. version stacks are more likely to change since
    // they were explicitly created to be edited)
    Predictive,
    // Complete scans happen less frequently and scan every item that is in the cache
    Complete,
    // Deep scans are NYI - they will search for new items in the cache root
    Deep
}
