#AutoCAD IO V2 API C# samples -- Create custom Activity with network access

[![.net](https://img.shields.io/badge/.net-4.5-green.svg)](http://www.microsoft.com/en-us/download/details.aspx?id=30653)
[![odata](https://img.shields.io/badge/odata-4.0-yellow.svg)](http://www.odata.org/documentation/)
[![ver](https://img.shields.io/badge/AutoCAD.io-2.0.0-blue.svg)](https://developer.autodesk.com/api/autocadio/v2/)
[![License](http://img.shields.io/:license-mit-red.svg)](http://opensource.org/licenses/MIT)

##Description
This is C# sample demonstrates a custom activity that accesses a relational database while processing a drawing. 
Your code running on AutoCAD.IO cannot normally access the network since it runs in sandbox. However, AutoCAD.IO now provides an experimental feature that allows you to access predefined network resources. 
This is how it works:
1. Your Activity defines one of input arguments as 'variadic'. A 'variadic' argument is like any other except the AutoCAD.IO infrastructure does not try to use it prior to starting your code.
2. Your code uses the variadic argument by calling the acesHttpOperation function. The AutoCAD.IO infrastructure then makes the HTTP call on your behalf and provides the results back to you.

Note that your can only access HTTP resources on the network. This means, for example, you cannot make TCP/IP calls to a SQL database. You must wrap the database with a REST API. 
This sample does not show you how to do the wrapping but there are lot of good resources on the internet. One helpful search keyword is 'Odata'.

##Dependencies

Visual Studio 2013.

##Setup/Usage Instructions

Please refer to [AutoCAD.IO V2 API documentation](https://developer.autodesk.com/api/autocadio/v2/#tutorials).

## Questions

Please post your question at our [forum](http://forums.autodesk.com/t5/autocad-i-o/bd-p/105).

## License

These samples are licensed under the terms of the [MIT License](http://opensource.org/licenses/MIT). Please see the [LICENSE](LICENSE) file for full details.

##Written by 

Albert Szilvasy