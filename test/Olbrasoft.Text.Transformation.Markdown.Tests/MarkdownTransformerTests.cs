using Moq;
using Xunit;

namespace Olbrasoft.Text.Transformation.Markdown.Tests;

public class MarkdownTransformerTests
{
    [Fact]
    public void TransformToHtml_WithValidMarkdown_ReturnsHtml()
    {
        // Arrange
        var markdown = new HeyRed.MarkdownSharp.Markdown();
        var document = new HtmlAgilityPack.HtmlDocument();
        var transformer = new DependentOnMarkdownAndHtmlAgilityPackMarkdownTransformer(markdown, document);

        // Act
        var result = transformer.TransformToHtml("**bold**");

        // Assert
        Assert.Contains("<strong>", result);
        Assert.Contains("bold", result);
    }

    [Fact]
    public void TransformToPlainText_WithValidMarkdown_ReturnsPlainText()
    {
        // Arrange
        var markdown = new HeyRed.MarkdownSharp.Markdown();
        var document = new HtmlAgilityPack.HtmlDocument();
        var transformer = new DependentOnMarkdownAndHtmlAgilityPackMarkdownTransformer(markdown, document);

        // Act
        var result = transformer.TransformToPlainText("**bold**");

        // Assert
        Assert.Equal("bold", result.Trim());
    }

    [Fact]
    public void TransformToHtml_WithEmptyString_ReturnsEmptyString()
    {
        // Arrange
        var markdown = new HeyRed.MarkdownSharp.Markdown();
        var document = new HtmlAgilityPack.HtmlDocument();
        var transformer = new DependentOnMarkdownAndHtmlAgilityPackMarkdownTransformer(markdown, document);

        // Act
        var result = transformer.TransformToHtml("");

        // Assert
        Assert.Equal("", result);
    }
}