namespace Olbrasoft.Text.Transformation.Markdown
{
    public interface IMarkdownTransformer
    {
        string TransformToHtml(string markdown);

        string TransformToPlainText(string markdown);
    }
}