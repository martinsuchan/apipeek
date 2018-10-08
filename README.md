# ApiPeek

Library for creating and comparing API dumps, from different versions of Windows 10.  
[Sample API diff between Windows 10 1803 -> 1809](https://martinsuchan.github.io/ApiPeek/Diffs/win10.1803.to.win10.1809.fulldiff.html).  
[Simplified API diff between Windows 10 1803 -> 1809 with only new types](https://martinsuchan.github.io/ApiPeek/Diffs/win10.1803.to.win10.1809.diff.html).  
**ApiPeek library** can be used in Windows Phone 8.1+ and Windows 8.1+ projects.  
**ApiPeek.Compare tool** is a simple Console tool in .NET 4.5.2.

## The motivation

ApiPeek library was created in the Fall of 2014. My goal was to create a simple library for traversing all available OS API on
the target device using Reflection and save the result as a group of simple JSON files, each representing one assembly,
zipped together as a single ZIP file.  
  
The second goal of this project was a tool for comparing two API dumps from two different OS versions.
Initially I was able to create simple txt files with API diffs,
later I upgraded the Comparer tool to generate sexy [HTML tree diffs with collapsible sections](https://martinsuchan.github.io/ApiPeek/Diffs/win10.14257.to.win10.14267.fulldiff.html).

## Building the project

The app is written in C# 6.0 and requires Visual Studio 2015 with Windows Store Tools installed.  
The app also requires NuGet package Json.NET 8.0.2, that is downloaded automatically during the build process.

## Creating API dumps

Just build the **ApiPeek.App.WindowsPhone** project and run it on your phone or the emulator.  
Click the **Start API Peek** button and wait couple of seconds.  
**File Save Picker** shows up, select where you want to save the API dump = .zip file.  
The same approach works on Windows 8.1.

## Comparing two API dumps

Currently the API compare process needs some manual steps.  
It's necessary to unzip both API dumps inside the **ApiPeek.Compare.App.Console** project inside the **api** folder.
Don't forget to set the property **Copy to Output Directory** on extracted files to **Always**.
Then select the target folder names in the **Program.cs** like this and start the Console app:

```
string path1 = "win10.10586";
string path2 = "win10.14267";
```

The Console app runs for only few seconds and then it automatically saves the diff inside the **bin/[Debug|Release]/html** folder.  

All API diffs I created so far are available here:  https://github.com/martinsuchan/martinsuchan.github.io/tree/master/ApiPeek/Diffs  
and can be browsed like this: https://martinsuchan.github.io/ApiPeek/Diffs/win10.14257.to.win10.14267.fulldiff.html

## Known issues

First of all this tool was created as a weekend hackathon project and the code documentation is mostly non-existent,
so please be warned before digging deeper how I done it :)  
I plan to update the documentation some time in the future + also delete some no longer used pieces of code.

Currently the ApiPeek library does not collect all information about the available API
**Not supported** features right now when traversing the API (non-exhaustive list):
 - overriden operators
 - constrains on generics
 - attributes on types
 - nested types
 
## References

- [Evolving the Reflection API](https://blogs.msdn.microsoft.com/dotnet/2012/08/28/evolving-the-reflection-api/)
- [Pure CSS collapsible tree menu](http://www.thecssninja.com/css/css-tree-menu)

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details
