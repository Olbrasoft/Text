# Olbrasoft.Text

[![Build & Publish](https://github.com/Olbrasoft/Text/actions/workflows/publish-nuget.yml/badge.svg)](https://github.com/Olbrasoft/Text/actions/workflows/publish-nuget.yml)
[![License](https://img.shields.io/badge/license-MIT-green)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-8.0%20%7C%209.0%20%7C%2010.0-blue)](https://dotnet.microsoft.com/)

Text transformation, translation and markdown processing libraries for .NET.

## Projects

| Project | Description | NuGet |
|---------|-------------|-------|
| [Olbrasoft.Text.Transformation.Markdown](#markdown-transformation) | Markdown to HTML/PlainText transformation | [![NuGet](https://img.shields.io/nuget/v/Olbrasoft.Text.Transformation.Markdown.svg)](https://www.nuget.org/packages/Olbrasoft.Text.Transformation.Markdown/) [![NuGet Downloads](https://img.shields.io/nuget/dt/Olbrasoft.Text.Transformation.Markdown.svg)](https://www.nuget.org/packages/Olbrasoft.Text.Transformation.Markdown/) |

---

## Markdown Transformation

A .NET library providing Markdown transformation utilities including `IMarkdownTransformer` interface, `MarkdownTransformer` implementation, and a `MarkdownTagHelper` for ASP.NET Core MVC views.

### Features

- **IMarkdownTransformer Interface** - Abstraction for Markdown transformation operations
- **MarkdownTransformer** - Implementation using Markdown library and HtmlAgilityPack
- **MarkdownTagHelper** - Razor TagHelper for rendering Markdown content in views
- **Dependency Injection Support** - Easy integration with ASP.NET Core DI container
- **Multi-framework Support** - Targets .NET Standard 2.0, .NET 8.0, 9.0, and 10.0

### Installation

```bash
dotnet add package Olbrasoft.Text.Transformation.Markdown
```

Or via Package Manager:

```powershell
Install-Package Olbrasoft.Text.Transformation.Markdown
```

### Quick Start

#### 1. Register Services

Edit your `Program.cs` or `Startup.cs`:

```csharp
using Olbrasoft.Text.Transformation.Markdown;

// In Program.cs (.NET 6+)
builder.Services.AddTextTransformationMarkdown();

// Or in Startup.cs (.NET Core 3.1)
public void ConfigureServices(IServiceCollection services)
{
    services.AddTextTransformationMarkdown();
}
```

#### 2. Configure TagHelper

Edit `_ViewImports.cshtml`:

```diff
@addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers
+ @addTagHelper *, Olbrasoft.Text.Transformation.Markdown
```

#### 3. Use in Views

```html
<markdown raw="true">**Bold text** and *italic text*</markdown>

<markdown>
# Heading 1

This is a paragraph with **bold** and *italic* text.

- List item 1
- List item 2
</markdown>
```

#### 4. Use via Dependency Injection

```csharp
public class MyService
{
    private readonly IMarkdownTransformer _markdownTransformer;

    public MyService(IMarkdownTransformer markdownTransformer)
    {
        _markdownTransformer = markdownTransformer;
    }

    public string TransformToHtml(string markdown)
    {
        return _markdownTransformer.ToHtml(markdown);
    }

    public string TransformToPlainText(string markdown)
    {
        return _markdownTransformer.ToPlainText(markdown);
    }
}
```

### Dependencies

- [Markdown](https://www.nuget.org/packages/Markdown/) - Markdown parsing library
- [HtmlAgilityPack](https://www.nuget.org/packages/HtmlAgilityPack/) - HTML parsing for plain text extraction
- [Microsoft.Extensions.DependencyInjection.Abstractions](https://www.nuget.org/packages/Microsoft.Extensions.DependencyInjection.Abstractions/) - DI support

### Multi-Targeting Support

| Target Framework | ASP.NET Core Support |
|------------------|---------------------|
| .NET Standard 2.0 | Microsoft.AspNetCore.Razor 2.2.0 |
| .NET 8.0 | FrameworkReference |
| .NET 9.0 | FrameworkReference |
| .NET 10.0 | FrameworkReference |

---

## Building from Source

```bash
git clone https://github.com/Olbrasoft/Text.git
cd Text
dotnet restore
dotnet build
dotnet test
```

## License

This project is licensed under the **MIT License** - see the [LICENSE](LICENSE) file for details.

## Author

- **Jiri Tuma**
- **Company**: Olbrasoft
- **Repository**: [https://github.com/Olbrasoft/Text](https://github.com/Olbrasoft/Text)

---

![Olbrasoft Markdown Transformation](./olbrasoft-text-transformation-markdown.png)

**Copyright 2020-2025 Olbrasoft**
