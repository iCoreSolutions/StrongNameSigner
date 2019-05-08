Brutal.Dev.StrongNameSigner.Lib
===============================

This directory contains code to automatically create a NuGet package of 
Brutal.Dev.StrongNameSigner only containing the lib that can be included
in your application to use the API from this package. (The original package
has all files in the "build" directory of the NuGet package and is intended
if you actually want to strong name references of the project you're building).

To create a package just merge the version of the repository you want to build
and execute:

`build.ps1` 

using PowerShell.

To automatically publish the package to our internal NuGet feed, 
use:

`build.ps1 -target push -configuration release`
