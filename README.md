# XAML Reporting

![XAML Reporting Logo](https://github.com/lecode-official/xaml-reporting/blob/master/Documentation/Images/Banner.png "XAML Reporting Logo")

The XAML Reporting engine is a tool, which offers reporting tools based on XAML and has the functionality to export to XPS, XLS(X), CSV and PDF.

## Acknowledgements

This project was conceived and first implemented by [Lukas Rögner](https://github.com/lukasroegner).

The PDF export functionality of the XAML Reporting engine was implemented using [PDFSharp](http://pdfsharp.net/). The functionality used was still in beta and
unfortunately not included in later builds, therefore we added the library statically to the project, instead of integrating it using NuGet.
[PDFSharp](http://pdfsharp.net/) is open source under the MIT license, you can read the license file
[here](https://github.com/lecode-official/xaml-reporting/blob/master/System.Windows.Documents.Reporting/Libraries/LICENSE).

The XLS and XLSX export functionality of the XAML Reporting engine was implemented using [NPOI](https://npoi.codeplex.com/), which is open source and published
under the Apache License Version 2.0, which you can read [here](https://npoi.codeplex.com/license).

## Using the Project

The project is available on NuGet: https://www.nuget.org/packages/System.Windows.Documents.Reporting/.

```batch
PM> Install-Package System.Windows.Documents.Reporting
```

If you want to you can download and manually build the solution. The project was built using Visual Studio 2015. Basically any version of Visual Studio 2015 will
suffice, no extra plugins or tools are needed (except for the `System.Windows.Documents.Reporting.nuproj` project, which needs the
[NuBuild Project System](https://visualstudiogallery.msdn.microsoft.com/3efbfdea-7d51-4d45-a954-74a2df51c5d0) Visual Studio extension for building the NuGet
package). Just clone the Git repository, open the solution in Visual Studio, and build the solution.

```batch
git pull https://github.com/lecode-official/xaml-reporting.git
```

## Contributions

Currently we are not accepting any contributors, but if you want to help, we would greatly appreciate feedback and bug reports. To file a bug, please use GitHub's
issue system. Alternatively, you can clone the repository and send us a pull request.
