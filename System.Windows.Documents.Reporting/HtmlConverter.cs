
#region Using Directives

using AngleSharp.Dom.Html;
using AngleSharp.Parser.Html;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AngleSharp.Dom;
using System.Collections.Generic;

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
        /// Converts the specified HTML node into a flow document element.
        /// </summary>
        /// <param name="htmlNode">The HTML node that is to be converted into a flow document element.</param>
        /// <param name="cancellationToken">The cancellation token, which can be used to cancel the conversion.</param>
        /// <returns>Returns the converted flow document element.</returns>
        private static Task<TextElement> ConvertHtmlNodeAsync(INode htmlNode, CancellationToken cancellationToken)
        {
            // Creates a new task, which runs in the background and converts the HTML element
            return Task<TextElement>.Run(async () =>
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
                            IEnumerable<Inline> paragraphContent = (await Task.WhenAll(htmlElement.ChildNodes.Select(child => HtmlConverter.ConvertHtmlNodeAsync(child, cancellationToken)))).OfType<Inline>();
                            Paragraph paragraph = new Paragraph();
                            paragraph.Inlines.AddRange(paragraphContent);
                            return paragraph;
                        case "SPAN":
                            IEnumerable<Inline> spanContent = (await Task.WhenAll(htmlElement.ChildNodes.Select(child => HtmlConverter.ConvertHtmlNodeAsync(child, cancellationToken)))).OfType<Inline>();
                            Span span = new Span();
                            span.Inlines.AddRange(spanContent);
                            return span;
                        case "EM":
                            IEnumerable<Inline> emphasisContent = (await Task.WhenAll(htmlElement.ChildNodes.Select(child => HtmlConverter.ConvertHtmlNodeAsync(child, cancellationToken)))).OfType<Inline>();
                            Span emphasis = new Span();
                            emphasis.FontStyle = FontStyles.Italic;
                            emphasis.Inlines.AddRange(emphasisContent);
                            return emphasis;
                        case "STRONG":
                            IEnumerable<Inline> strongContent = (await Task.WhenAll(htmlElement.ChildNodes.Select(child => HtmlConverter.ConvertHtmlNodeAsync(child, cancellationToken)))).OfType<Inline>();
                            Span strong = new Span();
                            strong.FontWeight = FontWeights.Bold;
                            strong.Inlines.AddRange(strongContent);
                            return strong;
                        case "A":
                            IEnumerable<Inline> hyperlinkContent = (await Task.WhenAll(htmlElement.ChildNodes.Select(child => HtmlConverter.ConvertHtmlNodeAsync(child, cancellationToken)))).OfType<Inline>();
                            Hyperlink hyperlink = new Hyperlink();
                            hyperlink.Inlines.AddRange(hyperlinkContent);
                            string hyperReference = htmlElement.GetAttribute("href");
                            Uri navigationUri;
                            if (Uri.TryCreate(hyperReference, UriKind.RelativeOrAbsolute, out navigationUri))
                                hyperlink.NavigateUri = navigationUri;
                            return hyperlink;
                    }
                }

                // Since the HTML node was either not an HTML element or the HTML element is not supported, the textual content of the HTML node is returned as a run element
                return new Run(htmlNode.TextContent) as TextElement;
            }, cancellationToken);
        }

        #endregion
        
        #region Public Static Methods

        /// <summary>
        /// Converts the HTML in the specified stream into a flow document.
        /// </summary>
        /// <param name="stream">The stream that contains the HTML that is to be converted.</param>
        /// <param name="cancellationToken">The cancellation token, which can be used to cancel the parsing and converting of the HTML.</param>
        /// <exception cref="InvalidOperationException">If the HTML could not be parsed, then an <see cref="InvalidOperationException"/> exception is thrown.</exception>
        /// <returns>Returns the converted flow document.</returns>
        public static async Task<FlowDocument> ConvertFromStream(Stream stream, CancellationToken cancellationToken)
        {
            // Tries to parse the HTML, if it could not be parsed, then an exception is thrown
            IHtmlDocument htmlDocument;
            try
            {
                htmlDocument = await new HtmlParser().ParseAsync(stream);
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException("The HTML could not be parsed.", exception);
            }

            // Creates a new flow document
            FlowDocument flowDocument = new FlowDocument();

            // Converts the HTML to block items, which are added to the flow document
            flowDocument.Blocks.AddRange(await Task.WhenAll(htmlDocument.Body.ChildNodes.Select(childNode => HtmlConverter.ConvertHtmlNodeAsync(childNode, cancellationToken))));

            // Returns the created flow document
            return flowDocument;
        }

        /// <summary>
        /// Converts the HTML in the specified stream into a flow document.
        /// </summary>
        /// <param name="stream">The stream that contains the HTML that is to be converted.</param>
        /// <exception cref="InvalidOperationException">If the HTML could not be parsed, then an <see cref="InvalidOperationException"/> exception is thrown.</exception>
        /// <returns>Returns the converted flow document.</returns>
        public static Task<FlowDocument> ConvertFromStream(Stream stream) => HtmlConverter.ConvertFromStream(stream, new CancellationTokenSource().Token);

        /// <summary>
        /// Downloads the HTML from the specified URI and converts it into a flow document.
        /// </summary>
        /// <param name="uri">The URI from which the HTML is to be loaded.</param>
        /// <param name="cancellationToken">The cancellation token, which can be used to cancel the parsing and converting of the HTML.</param>
        /// <exception cref="InvalidOperationException">If the HTML could not be downloaded or parsed, then an <see cref="InvalidOperationException"/> exception is thrown.</exception>
        /// <returns>Returns the converted flow document.</returns>
        public static async Task<FlowDocument> ConvertFromUri(Uri uri, CancellationToken cancellationToken)
        {
            // Tries to download the HTML from the specified URI, if it could not be loaded, then an exception is thrown
            try
            {
                using (HttpClient httpClient = new HttpClient())
                {
                    HttpResponseMessage responseMessage = await httpClient.GetAsync(uri, cancellationToken);
                    responseMessage.EnsureSuccessStatusCode();
                    return await HtmlConverter.ConvertFromStream(await responseMessage.Content.ReadAsStreamAsync(), cancellationToken);
                }
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException("The HTML page could not be loaded.", exception);
            }
        }

        /// <summary>
        /// Downloads the HTML from the specified URI and converts it into a flow document.
        /// </summary>
        /// <param name="uri">The URI from which the HTML is to be loaded.</param>
        /// <exception cref="InvalidOperationException">If the HTML could not be downloaded or parsed, then an <see cref="InvalidOperationException"/> exception is thrown.</exception>
        /// <returns>Returns the converted flow document.</returns>
        public static Task<FlowDocument> ConvertFromUri(Uri uri) => HtmlConverter.ConvertFromUri(uri, new CancellationTokenSource().Token);

        /// <summary>
        /// Converts the specified HTML file into a flow document.
        /// </summary>
        /// <param name="fileName">The name of the HTML file, that is to be loaded and converted into a flow document.</param>
        /// <param name="cancellationToken">The cancellation token, which can be used to cancel the parsing and converting of the HTML.</param>
        /// <exception cref="InvalidOperationException">If the HTML could not be parsed, then an <see cref="InvalidOperationException"/> exception is thrown.</exception>
        /// <exception cref="FileNotFoundException">If the specified file could not be found, then a <see cref="FileNotFoundException"/> exception is thrown.</exception>
        /// <returns>Returns the converted flow document.</returns>
        public static Task<FlowDocument> ConvertFromFile(string fileName, CancellationToken cancellationToken)
        {
            // Checks if the file exists, if not then an exception is thrown
            if (!File.Exists(fileName))
                throw new FileNotFoundException("The HTML file could not be found.", fileName);

            // Loads the file, converts it into a flow document, and returns the converted flow document
            using (FileStream fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read))
                return HtmlConverter.ConvertFromStream(fileStream, cancellationToken);
        }

        /// <summary>
        /// Converts the specified HTML file into a flow document.
        /// </summary>
        /// <param name="fileName">The name of the HTML file, that is to be loaded and converted into a flow document.</param>
        /// <exception cref="InvalidOperationException">If the HTML could not be parsed, then an <see cref="InvalidOperationException"/> exception is thrown.</exception>
        /// <exception cref="FileNotFoundException">If the specified file could not be found, then a <see cref="FileNotFoundException"/> exception is thrown.</exception>
        /// <returns>Returns the converted flow document.</returns>
        public static Task<FlowDocument> ConvertFromFile(string fileName) => HtmlConverter.ConvertFromFile(fileName, new CancellationTokenSource().Token);

        /// <summary>
        /// Converts the specified HTML into a flow document.
        /// </summary>
        /// <param name="html">The HTML string that is to be converted into a flow document.</param>
        /// <param name="cancellationToken">The cancellation token, which can be used to cancel the parsing and converting of the HTML.</param>
        /// <exception cref="InvalidOperationException">If the HTML could not be parsed, then an <see cref="InvalidOperationException"/> exception is thrown.</exception>
        /// <returns>Returns the converted flow document.</returns>
        public static Task<FlowDocument> ConvertFromString(string html, CancellationToken cancellationToken)
        {
            // Converts the string into a memory stream, converts the HTML into a flow document, and returns it
            using (MemoryStream memoryStream = new MemoryStream(Encoding.Unicode.GetBytes(html)))
                return HtmlConverter.ConvertFromStream(memoryStream, cancellationToken);
        }

        /// <summary>
        /// Converts the specified HTML into a flow document.
        /// </summary>
        /// <param name="html">The HTML string that is to be converted into a flow document.</param>
        /// <exception cref="InvalidOperationException">If the HTML could not be parsed, then an <see cref="InvalidOperationException"/> exception is thrown.</exception>
        /// <returns>Returns the converted flow document.</returns>
        public static Task<FlowDocument> ConvertFromString(string html) => HtmlConverter.ConvertFromString(html, new CancellationTokenSource().Token);

        #endregion
    }
}