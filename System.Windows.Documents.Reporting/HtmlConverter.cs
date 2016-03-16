
#region Using Directives

using AngleSharp.Dom;
using AngleSharp.Dom.Html;
using AngleSharp.Parser.Html;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

#endregion

namespace System.Windows.Documents.Reporting
{
    /// <summary>
    /// Represents a simple HTML converter, which parses the HTML and convertes it to a flow document, which can then be used to renders PDFs or XPSs.
    /// </summary>
    public class HtmlConverter
    {
        #region Private Static Methods

        /// <summary>
        /// Converts the specified HTML node into flow document content element.
        /// </summary>
        /// <param name="htmlNode">The HTML node that is to be converted into flow document content element.</param>
        /// <param name="cancellationToken">The cancellation token, which can be used to cancel the conversion.</param>
        /// <returns>Returns the converted flow document element.</returns>
        private static TextElement ConvertHtmlNode(INode htmlNode)
        {
            // Checks if the HTML node is an HTML element, in that case the HTML element is parsed
            IHtmlElement htmlElement = htmlNode as IHtmlElement;
            if (htmlElement != null)
            {
                switch (htmlElement.NodeName.ToUpperInvariant())
                {
                    case "BR":
                        return new LineBreak();
                    case "P":
                        IEnumerable<Inline> paragraphContent = htmlElement.ChildNodes.Select(child => HtmlConverter.ConvertHtmlNode(child)).OfType<Inline>();
                        Paragraph paragraph = new Paragraph();
                        paragraph.Inlines.AddRange(paragraphContent);
                        return paragraph;
                    case "SPAN":
                        IEnumerable<Inline> spanContent = htmlElement.ChildNodes.Select(child => HtmlConverter.ConvertHtmlNode(child)).OfType<Inline>();
                        Span span = new Span();
                        span.Inlines.AddRange(spanContent);
                        return span;
                    case "EM":
                        IEnumerable<Inline> emphasisContent = htmlElement.ChildNodes.Select(child => HtmlConverter.ConvertHtmlNode(child)).OfType<Inline>();
                        Span emphasis = new Span();
                        emphasis.FontStyle = FontStyles.Italic;
                        emphasis.Inlines.AddRange(emphasisContent);
                        return emphasis;
                    case "STRONG":
                        IEnumerable<Inline> strongContent = htmlElement.ChildNodes.Select(child => HtmlConverter.ConvertHtmlNode(child)).OfType<Inline>();
                        Span strong = new Span();
                        strong.FontWeight = FontWeights.Bold;
                        strong.Inlines.AddRange(strongContent);
                        return strong;
                    case "A":
                        IEnumerable<Inline> hyperlinkContent = htmlElement.ChildNodes.Select(child => HtmlConverter.ConvertHtmlNode(child)).OfType<Inline>();
                        Hyperlink hyperlink = new Hyperlink();
                        hyperlink.Inlines.AddRange(hyperlinkContent);
                        string hyperReference = htmlElement.GetAttribute("href");
                        Uri navigationUri;
                        if (Uri.TryCreate(hyperReference, UriKind.RelativeOrAbsolute, out navigationUri))
                            hyperlink.NavigateUri = navigationUri;
                        return hyperlink;
                    case "H1":
                    case "H2":
                    case "H3":
                    case "H4":
                    case "H5":
                    case "H6":
                        IEnumerable<Inline> headingContent = htmlElement.ChildNodes.Select(child => HtmlConverter.ConvertHtmlNode(child)).OfType<Inline>();
                        Paragraph heading = new Paragraph();
                        double fontSize = new double[] { 24, 18, 13.5, 12, 10, 7.5 }[Convert.ToInt32(htmlElement.NodeName.Substring(1)) - 1];
                        heading.FontSize = fontSize;
                        heading.Inlines.AddRange(headingContent);
                        heading.Margin = new Thickness(double.NaN, double.NaN, double.NaN, 0.0d);
                        return heading;
                }
            }

            // Since the HTML node was either not an HTML element or the HTML element is not supported, the textual content of the HTML node is returned as a run element
            return new Run(htmlNode.TextContent);
        }

        #endregion
        
        #region Public Static Methods

        /// <summary>
        /// Converts the HTML in the specified stream into flow document content.
        /// </summary>
        /// <param name="stream">The stream that contains the HTML that is to be converted.</param>
        /// <param name="cancellationToken">The cancellation token, which can be used to cancel the parsing and converting of the HTML.</param>
        /// <exception cref="InvalidOperationException">If the HTML could not be parsed, then an <see cref="InvalidOperationException"/> exception is thrown.</exception>
        /// <returns>Returns the converted flow document content.</returns>
        public static async Task<Section> ConvertFromStreamAsync(Stream stream, CancellationToken cancellationToken)
        {
            // Tries to parse the HTML, if it could not be parsed, then an exception is thrown
            IHtmlDocument htmlDocument;
            try
            {
                HtmlParser htmlParser = new HtmlParser();
                htmlDocument = await new HtmlParser().ParseAsync(stream);
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException(Resources.Localization.HtmlConverter.HtmlCouldNotBeParsedExceptionMessage, exception);
            }

            // Creates a new section, which holds the content of the 
            Section flowDocumentContent = new Section();

            // Converts the HTML to block items, which are added to the section (elements that are not a block have to be wrapped in a paragraph, otherwise the section will not accept them as content)
            IEnumerable<TextElement> textElements = htmlDocument.Body.ChildNodes.Select(childNode => HtmlConverter.ConvertHtmlNode(childNode)).ToList();
            List<Block> blockElements = new List<Block>();
            Paragraph currentContainerParagraph = null;
            foreach (TextElement textElement in textElements)
            {
                Block block = textElement as Block;
                if (block != null)
                {
                    if (currentContainerParagraph != null)
                    {
                        blockElements.Add(currentContainerParagraph);
                        currentContainerParagraph = null;
                    }
                    blockElements.Add(block);
                }
                else
                {
                    currentContainerParagraph = currentContainerParagraph ?? new Paragraph();
                    currentContainerParagraph.Inlines.Add(textElement as Inline);
                }
            }
            if (currentContainerParagraph != null)
                blockElements.Add(currentContainerParagraph);
            flowDocumentContent.Blocks.AddRange(blockElements);

            // Returns the created flow document content
            return flowDocumentContent;
        }

        /// <summary>
        /// Converts the HTML in the specified stream into flow document content.
        /// </summary>
        /// <param name="stream">The stream that contains the HTML that is to be converted.</param>
        /// <exception cref="InvalidOperationException">If the HTML could not be parsed, then an <see cref="InvalidOperationException"/> exception is thrown.</exception>
        /// <returns>Returns the converted flow document content.</returns>
        public static Task<Section> ConvertFromStreamAsync(Stream stream) => HtmlConverter.ConvertFromStreamAsync(stream, new CancellationTokenSource().Token);

        /// <summary>
        /// Downloads the HTML from the specified URI and converts it into flow document content.
        /// </summary>
        /// <param name="uri">The URI from which the HTML is to be loaded.</param>
        /// <param name="cancellationToken">The cancellation token, which can be used to cancel the parsing and converting of the HTML.</param>
        /// <exception cref="InvalidOperationException">If the HTML could not be downloaded or parsed, then an <see cref="InvalidOperationException"/> exception is thrown.</exception>
        /// <returns>Returns the converted flow document content.</returns>
        public static async Task<Section> ConvertFromUriAsync(Uri uri, CancellationToken cancellationToken)
        {
            // Tries to download the HTML from the specified URI, if it could not be loaded, then an exception is thrown
            try
            {
                using (HttpClient httpClient = new HttpClient())
                {
                    HttpResponseMessage responseMessage = await httpClient.GetAsync(uri, cancellationToken);
                    responseMessage.EnsureSuccessStatusCode();
                    return await HtmlConverter.ConvertFromStreamAsync(await responseMessage.Content.ReadAsStreamAsync(), cancellationToken);
                }
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException(Resources.Localization.HtmlConverter.HtmlPageCouldNotBeLoadedExceptionMessage, exception);
            }
        }

        /// <summary>
        /// Downloads the HTML from the specified URI and converts it into flow document content.
        /// </summary>
        /// <param name="uri">The URI from which the HTML is to be loaded.</param>
        /// <exception cref="InvalidOperationException">If the HTML could not be downloaded or parsed, then an <see cref="InvalidOperationException"/> exception is thrown.</exception>
        /// <returns>Returns the converted flow document content.</returns>
        public static Task<Section> ConvertFromUriAsync(Uri uri) => HtmlConverter.ConvertFromUriAsync(uri, new CancellationTokenSource().Token);

        /// <summary>
        /// Converts the specified HTML file into flow document content.
        /// </summary>
        /// <param name="fileName">The name of the HTML file, that is to be loaded and converted into flow document content.</param>
        /// <param name="cancellationToken">The cancellation token, which can be used to cancel the parsing and converting of the HTML.</param>
        /// <exception cref="InvalidOperationException">If the HTML could not be parsed, then an <see cref="InvalidOperationException"/> exception is thrown.</exception>
        /// <exception cref="FileNotFoundException">If the specified file could not be found, then a <see cref="FileNotFoundException"/> exception is thrown.</exception>
        /// <returns>Returns the converted flow document content.</returns>
        public static Task<Section> ConvertFromFileAsync(string fileName, CancellationToken cancellationToken)
        {
            // Checks if the file exists, if not then an exception is thrown
            if (!File.Exists(fileName))
                throw new FileNotFoundException(Resources.Localization.HtmlConverter.HtmlFileCouldNotBeFoundExceptionMessage, fileName);

            // Loads the file, converts it into flow document content, and returns the converted flow document
            using (FileStream fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read))
                return HtmlConverter.ConvertFromStreamAsync(fileStream, cancellationToken);
        }

        /// <summary>
        /// Converts the specified HTML file into flow document content.
        /// </summary>
        /// <param name="fileName">The name of the HTML file, that is to be loaded and converted into flow document content.</param>
        /// <exception cref="InvalidOperationException">If the HTML could not be parsed, then an <see cref="InvalidOperationException"/> exception is thrown.</exception>
        /// <exception cref="FileNotFoundException">If the specified file could not be found, then a <see cref="FileNotFoundException"/> exception is thrown.</exception>
        /// <returns>Returns the converted flow document content.</returns>
        public static Task<Section> ConvertFromFileAsync(string fileName) => HtmlConverter.ConvertFromFileAsync(fileName, new CancellationTokenSource().Token);

        /// <summary>
        /// Converts the specified HTML into flow document content.
        /// </summary>
        /// <param name="html">The HTML string that is to be converted into flow document content.</param>
        /// <param name="cancellationToken">The cancellation token, which can be used to cancel the parsing and converting of the HTML.</param>
        /// <exception cref="InvalidOperationException">If the HTML could not be parsed, then an <see cref="InvalidOperationException"/> exception is thrown.</exception>
        /// <returns>Returns the converted flow document content.</returns>
        public static Task<Section> ConvertFromStringAsync(string html, CancellationToken cancellationToken)
        {
            // Converts the string into a memory stream, converts the HTML into flow document content, and returns it
            using (MemoryStream memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(html)))
                return HtmlConverter.ConvertFromStreamAsync(memoryStream, cancellationToken);
        }

        /// <summary>
        /// Converts the specified HTML into flow document content.
        /// </summary>
        /// <param name="html">The HTML string that is to be converted into flow document content.</param>
        /// <exception cref="InvalidOperationException">If the HTML could not be parsed, then an <see cref="InvalidOperationException"/> exception is thrown.</exception>
        /// <returns>Returns the converted flow document content.</returns>
        public static Task<Section> ConvertFromStringAsync(string html) => HtmlConverter.ConvertFromStringAsync(html, new CancellationTokenSource().Token);

        #endregion
    }
}