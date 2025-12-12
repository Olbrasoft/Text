using HtmlAgilityPack;
using Microsoft.Extensions.DependencyInjection;

namespace Olbrasoft.Text.Transformation.Markdown
{
    public static class ServiceCollectionExtensions
    {
        public static void AddTextTransformationMarkdown(this IServiceCollection services)
        {
            services.AddSingleton<HtmlDocument>();
            services.AddSingleton<HeyRed.MarkdownSharp.Markdown>();
            services.AddSingleton<IMarkdownTransformer, DependentOnMarkdownAndHtmlAgilityPackMarkdownTransformer>();
        }
    }
}