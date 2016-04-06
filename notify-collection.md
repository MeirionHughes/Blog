#Notify Collection 

Interfaces are an important part of .net and are an important aspect of collections

For example:

```cs
ICollection<string> myItems = new List<string>();
```

with net 4.5 we now also get `IReadOnlyCollection<T>` and `IReadOnlyList<T>`.
These are a powerful addition, which most built-in collections implement. 
It allows one to hold a writable collection, but only expose the read-only aspects of it. i.e. 


```cs
IList<string> items = new List<string>();
IReadOnlyList<string> readOnlyItems = items;

items.Add("Foo");

Assert.That(readOnlyItems[0] == "Foo") // true;
```

Another important collection interface is `INotifyCollectionChanged`, 
which should return events detailing the changes to the collection. 
There is a built in class `ObservableCollection<T>` that implements 'INotifyCollectionChanged' too. 
Unfortunatly there isn't a collection interface that merges `ICollection` and `INotifyCollectionChanged`, 
neither is there a read only equivelent. 

Fortunatly, its rather easy for us to make them:

```cs
public interface INotifyCollection<T> 
       : ICollection<T>, 
         INotifyCollectionChanged
{}
```

```cs
IReadOnlyNotifyCollection.cs
public interface IReadOnlyNotifyCollection<out T> 
       : IReadOnlyCollection<T>, 
         INotifyCollectionChanged
{}
```

```cs
NotifyCollection.cs
public class NotifyCollection<T> 
       : ObservableCollection<T>, 
         INotifyCollection<T>, 
         IReadOnlyNotifyCollection<T>
{}

```

These can be used like so: 

```cs
var full = new NotifyCollection<string>();
var readOnlyAccess = (IReadOnlyCollection<string>) full;
var readOnlyNotifyOfChange = (IReadOnlyNotifyCollection<string>) full;


//Covarience
var readOnlyListWithChanges = 
    new List<IReadOnlyNotifyCollection<object>>()
        {
            new NotifyCollection<object>(),
            new NotifyCollection<string>(),
        };
```




