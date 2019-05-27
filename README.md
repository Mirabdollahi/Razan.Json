# Razan.Json
A .NET implementation for JSON (JavaScript Object Notation)

## Installation
Package is avaliable via [NuGet](https://www.nuget.org/packages/Razan.Json). Or you can download and compile it yourself.

## Supported .NET versions:
- .NET Standard 2.0

## Usage

```C#
var json = new JsonObject(); // JsonObject implements IDictionary<string, object>
json.AsDynamic.Array = new[] { 1, 2, 3, 4 };
json.AsDynamic.Object = new
{
    Prop1 = true,
    Prop2 = 10d,
    Prop3 = new[] { "Str1", "Str2" }
};

var array = json.OptJsonArray("Array"); // JsonArray implements List<object>
array.Add(5);

var jsonArrayOfInt = array.AsJsonArray<int>(); // JsonArray<TType> implements List<TType>
jsonArrayOfInt.Add(6);

var readOnlyJsonArray = array.AsReadOnly; // ReadOnlyJsonArray implements IReadOnlyList<object>

Console.WriteLine(json.ToString());
```

The output would be:

>{"Array":[1,2,3,4,5,6],"Object":{"Prop1":true,"Prop2":10,"Prop3":["Str1","Str2"]}}

```C#
var stringified = json.ToString(true); // true to comment out data types for safe type imports

Console.WriteLine(stringified);
```

The output would be:

>{"Array":[1/\*int*/,2/\*int*/,3/\*int*/,4/\*int*/,5/\*int*/,6/\*int*/],"Object":{"Prop1":true,"Prop2":1/*double*/,"Prop3":["Str1","Str2"]}}

```C#
dynamic importedJson = new JsonObject(stringified);

Console.WriteLine(importedJson.Object.Prop2.GetType().FullName);
```

The output would be:

>System.Double

```C#
var prop2AsInt32 = ((JsonObject)importedJson.Object).OptInt32("Prop2");

Console.WriteLine(prop2AsInt32.GetType().FullName);
```

The output would be:

>System.Int32
