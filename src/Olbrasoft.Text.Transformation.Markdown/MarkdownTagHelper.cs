using Microsoft.AspNetCore.Razor.TagHelpers;
using System.Threading.Tasks;

namespace Olbrasoft.Text.Transformation.Markdown
{
    [HtmlTargetElement("markdown")]
    public class MarkdownTagHelper : TagHelper
    {
        private readonly IMarkdownTransformer _transformer;

        [HtmlAttributeName("raw")]
        public bool Raw { get; set; }

        public MarkdownTagHelper(IMarkdownTransformer transformer)
        {
            _transformer = transformer;
        }

        public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            output.TagName = null;

            var content = (await output.GetChildContentAsync()).GetContent();

            if (Raw) output.Content.SetHtmlContent(_transformer.TransformToHtml(content));
            else
                output.Content.SetHtmlContent(_transformer.TransformToPlainText(content));
        }
    }
}