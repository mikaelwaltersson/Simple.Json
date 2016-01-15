# Simple.Json

Easy to use framework for serializing to and from json. Supports typed dto objects, dynamic dto objects and immutable dto types like F# record types.

Supports Microsoft Web API out of the box (MediaTypeFormatter implementation).



**Initial release, version 1.0 (November 30, 2012)**

**Available through NuGet:** http://www.nuget.org/List/Packages/Simple.Mocking


## Fast serialization and deserialization

Time needed for serialization/deserialization of *sample1.json* 1000000 times.
(Produced by FrameworkPerformanceTests included in the source code)

* _ServiceStack.Text - deserialize: 15.502s_
* _ServiceStack.Text - serialize: 7.819s_
* _Json.NET - deserialize: 18.890s_
* _Json.NET - serialize: 12.158s_
* _Simple.Json - deserialize: 8.016s_
* _Simple.Json - serialize: 7.577s_



## TODO

* Enum serialization 
* DateTimeOffset serialization
* Discriminator support
* Mandatory fields validation (counter not correct)

