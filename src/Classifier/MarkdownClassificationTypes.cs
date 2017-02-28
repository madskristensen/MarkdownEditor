using Microsoft.VisualStudio.Language.StandardClassification;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;
using System.Windows;
using System.Windows.Media;

namespace MarkdownEditor
{
    internal static class MarkdownClassificationTypes
    {
        public const string MarkdownBold = "md_bold";
        public const string MarkdownItalic = "md_italic";
        public const string MarkdownHeader = "md_header";
        public const string MarkdownCode = "md_code";
        public const string MarkdownQuote = "md_quote";
        public const string MarkdownHtml = "md_html";
        public const string MarkdownLink = PredefinedClassificationTypeNames.Keyword;
        public const string MarkdownComment = PredefinedClassificationTypeNames.Comment;
        public const string MarkdownNaturalLanguage = PredefinedClassificationTypeNames.NaturalLanguage;

        [Export, Name(MarkdownBold)]
        public static ClassificationTypeDefinition MarkdownClassificationBold { get; set; }

        [Export, Name(MarkdownItalic)]
        public static ClassificationTypeDefinition MarkdownClassificationItalic { get; set; }

        [Export, Name(MarkdownHeader)]
        public static ClassificationTypeDefinition MarkdownClassificationHeader { get; set; }

        [Export, Name(MarkdownCode)]
        public static ClassificationTypeDefinition MarkdownClassificationCode { get; set; }

        [Export, Name(MarkdownQuote)]
        public static ClassificationTypeDefinition MarkdownClassificationQuote { get; set; }

        [Export, Name(MarkdownHtml)]
        public static ClassificationTypeDefinition MarkdownClassificationHtml { get; set; }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = MarkdownClassificationTypes.MarkdownBold)]
    [Name(MarkdownClassificationTypes.MarkdownBold)]
    internal sealed class MarkdownBoldFormatDefinition : ClassificationFormatDefinition
    {
        public MarkdownBoldFormatDefinition()
        {
            IsBold = true;
            DisplayName = "Markdown Bold";
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = MarkdownClassificationTypes.MarkdownItalic)]
    [Name(MarkdownClassificationTypes.MarkdownItalic)]
    internal sealed class MarkdownItalicFormatDefinition : ClassificationFormatDefinition
    {
        public MarkdownItalicFormatDefinition()
        {
            IsItalic = true;
            DisplayName = "Markdown Italic";
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = MarkdownClassificationTypes.MarkdownHeader)]
    [Name(MarkdownClassificationTypes.MarkdownHeader)]
    [UserVisible(true)]
    internal sealed class MarkdownHeaderFormatDefinition : ClassificationFormatDefinition
    {
        public MarkdownHeaderFormatDefinition()
        {
            IsBold = true;
            TextDecorations = new TextDecorationCollection();
            TextDecorations.Add(new TextDecoration());
            DisplayName = "Markdown Headers";
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = MarkdownClassificationTypes.MarkdownCode)]
    [Name(MarkdownClassificationTypes.MarkdownCode)]
    [UserVisible(true)]
    internal sealed class MarkdownCodeFormatDefinition : ClassificationFormatDefinition
    {
        public MarkdownCodeFormatDefinition()
        {
            FontTypeface = new Typeface("Courier New");
            DisplayName = "Markdown Code";
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = MarkdownClassificationTypes.MarkdownQuote)]
    [Name(MarkdownClassificationTypes.MarkdownQuote)]
    [UserVisible(true)]
    internal sealed class MarkdownQuoteFormatDefinition : ClassificationFormatDefinition
    {
        public MarkdownQuoteFormatDefinition()
        {
            // I wish I could make the background apply block-level (to highlight the entire line)
            BackgroundColor = Colors.LightGray;
            BackgroundOpacity = .4;
            DisplayName = "Markdown Quote";
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = MarkdownClassificationTypes.MarkdownHtml)]
    [Name(MarkdownClassificationTypes.MarkdownHtml)]
    [UserVisible(true)]
    internal sealed class MarkdownHtmlFormatDefinition : ClassificationFormatDefinition
    {
        public MarkdownHtmlFormatDefinition()
        {
            ForegroundColor = Colors.Maroon;
            DisplayName = "Markdown HTML";
        }
    }
}
