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