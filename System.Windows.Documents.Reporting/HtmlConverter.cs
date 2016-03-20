
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
        /// Retrieves all the runs and line breaks inside an inline collection recursively. This is very useful for white-space character handling.
        /// </summary>
        /// <param name="inlineCollection">The inline collection from which the runs and line breaks are to be retrieved.</param>
        /// <returns>Returns a list of runs and line breaks from the specified inline collection in the order that they appear within the inline collection.</returns>
        private static List<Inline> GetRunsAndLineBreaks(InlineCollection inlineCollection)
        {
            // Creates a new list, which will contain the result
            List<Inline> runsAndLineBreaks = new List<Inline>();

            // Cycles over all the inlines in the inline collection
            foreach (Inline inline in inlineCollection)
            {
                // Checks if the current inline element is a run, if so then it is added to the result set
                Run run = inline as Run;
                if (run != null)
                    runsAndLineBreaks.Add(run);

                // Checks if the current inline element is a line break, if so then it is added to the result set
                LineBreak lineBreak = inline as LineBreak;
                if (lineBreak != null)
                    runsAndLineBreaks.Add(lineBreak);

                // Checks if the current inline element is a span, a span is the only inline element, which can itself contain inline elements, if the current
                // inline element is a span, then the runs and line breaks that it contains are recursively retrieved and added to the result set
                Span span = inline as Span;
                if (span != null)
                    runsAndLineBreaks.AddRange(HtmlConverter.GetRunsAndLineBreaks(span.Inlines));
            }

            // Returns the created list of runs and line breaks
            return runsAndLineBreaks;
        }

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
                    case "I":
                    case "EM":
                        IEnumerable<Inline> emphasisContent = htmlElement.ChildNodes.Select(child => HtmlConverter.ConvertHtmlNode(child)).OfType<Inline>();
                        Span emphasis = new Span();
                        emphasis.FontStyle = FontStyles.Italic;
                        emphasis.Inlines.AddRange(emphasisContent);
                        return emphasis;
                    case "B":
                    case "STRONG":
                        IEnumerable<Inline> boldContent = htmlElement.ChildNodes.Select(child => HtmlConverter.ConvertHtmlNode(child)).OfType<Inline>();
                        Span bold = new Span();
                        bold.FontWeight = FontWeights.Bold;
                        bold.Inlines.AddRange(boldContent);
                        return bold;
                    case "Q":
                        IEnumerable<Inline> quotationContent = htmlElement.ChildNodes.Select(child => HtmlConverter.ConvertHtmlNode(child)).OfType<Inline>();
                        Span quotation = new Span();
                        quotation.Inlines.Add(new Run("\""));
                        quotation.Inlines.AddRange(quotationContent);
                        quotation.Inlines.Add(new Run("\""));
                        return quotation;
                    case "S":
                    case "STRIKE":
                        IEnumerable<Inline> strikeThroughContent = htmlElement.ChildNodes.Select(child => HtmlConverter.ConvertHtmlNode(child)).OfType<Inline>();
                        Span strikeThrough = new Span();
                        strikeThrough.TextDecorations.Add(TextDecorations.Strikethrough);
                        strikeThrough.Inlines.AddRange(strikeThroughContent);
                        return strikeThrough;
                    case "U":
                        IEnumerable<Inline> underlineContent = htmlElement.ChildNodes.Select(child => HtmlConverter.ConvertHtmlNode(child)).OfType<Inline>();
                        Span underline = new Span();
                        underline.TextDecorations.Add(TextDecorations.Underline);
                        underline.Inlines.AddRange(underlineContent);
                        return underline;
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
            return new Run(Regex.Replace(htmlNode.TextContent, "\\s+", " "));
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

            // Converts the HTML to block items, which are added to the section (elements that are not a block have to be wrapped in a paragraph, otherwise the
            // section will not accept them as content)
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
                        if (currentContainerParagraph.Inlines.Count > 1 || !currentContainerParagraph.Inlines.OfType<Run>().Any(run => string.IsNullOrWhiteSpace(run.Text)))
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

            // Cylces over each paragraph in the list of block elements and applies the rules for white-space character handling to it
            foreach (Paragraph paragraph in blockElements.OfType<Paragraph>())
            {
                // Recursively retrieves all the runs and line breaks that are contained in the paragraph (runs and line breaks are the only inline elements that
                // are relevant for white-space character handling, because runs are the only inline elements that can directly contain text, and therefore
                // white-space characters, and all white space characters directly before and after line breaks must be ignored)
                List<Inline> runsAndLineBreaks = HtmlConverter.GetRunsAndLineBreaks(paragraph.Inlines);

                // Cycles over the list of retrieved runs and line breaks, till there are no more run left
                while (runsAndLineBreaks.OfType<Run>().Any())
                {
                    // Retrieves all runs till the first line break appears
                    List<Run> runs = runsAndLineBreaks.SkipWhile(inline => inline is LineBreak).TakeWhile(inline => inline is Run).OfType<Run>().ToList();
                    
                    // Applies the first rule of white-space character handling: there must only be one white space between two inline elements
                    for (int i = 0; i < runs.Count() - 1; i++)
                    {
                        if (runs[i].Text.EndsWith(" ") && runs[i + 1].Text.StartsWith(" "))
                            runs[i].Text = runs[i].Text.TrimEnd();
                    }

                    // Applies the second rule of white-space handling: the first non-empty run must not start with white-space characters (this is because it is
                    // the run that has the first textual content within the paragraph, which is a block element, and block elements must never begin with a
                    // white-space or it is the first run that has textual content after a line break and white-spaces after line breaks have to be ignored)
                    foreach (Run run in runs)
                    {
                        run.Text = run.Text.TrimStart();
                        if (!string.IsNullOrWhiteSpace(run.Text))
                            break;
                    }

                    // Applies the third rule of white-space handling: the last non-empty run must not end with white-space characters (this is because it is the
                    // run that has the last textual content within the paragraph, which is a block element, and block elements must never end with a white-space,
                    // or it is the last run before a line break and white-spaces before line-breaks have to be ignored)
                    foreach (Run run in runs.Reverse<Run>())
                    {
                        run.Text = run.Text.TrimEnd();
                        if (!string.IsNullOrWhiteSpace(run.Text))
                            break;
                    }

                    // Removes all the runs from the collection, because their white-spaces have already been handled
                    runsAndLineBreaks.RemoveAll(inline => runs.Contains(inline as Run));
                }
            }

            // Returns the created flow document content
            flowDocumentContent.Blocks.AddRange(blockElements);
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