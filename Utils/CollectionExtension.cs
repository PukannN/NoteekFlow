using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

public static class CollectionExtension
{
    public static ObservableCollection<string> ToObservableCollection(this IEnumerable<string> source)
    {
        return new ObservableCollection<string>(source);
    }
}