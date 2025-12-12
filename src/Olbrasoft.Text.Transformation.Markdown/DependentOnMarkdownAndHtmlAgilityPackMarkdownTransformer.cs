using HtmlAgilityPack;

namespace Olbrasoft.Text.Transformation.Markdown
{
    //https://www.nuget.org/packages/Markdown/
    //https://www.nuget.org/packages/HtmlAgilityPack/
    public class DependentOnMarkdownAndHtmlAgilityPackMarkdownTransformer : IMarkdownTransformer
    {
        private readonly HeyRed.MarkdownSharp.Markdown _markdown;
        private readonly HtmlDocument _document;

        public DependentOnMarkdownAndHtmlAgilityPackMarkdownTransformer(HeyRed.MarkdownSharp.Markdown markdown, HtmlDocument document)
        {
            _markdown = markdown;
            _document = document;
        }

        public string TransformToHtml(string markdown)
        {
            return _markdown.Transform(markdown);
        }

        public string TransformToPlainText(string markdown)
        {
            _document.LoadHtml(TransformToHtml(markdown));
            return _document.DocumentNode.InnerText;
        }
    }
}