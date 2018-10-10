using System.ComponentModel;
using Microsoft.VisualStudio.Shell;

namespace MarkdownEditor
{
    public class Options : DialogPage
    {
        // HTML Generation
        [Category("HTML Generation")]
        [DisplayName("Generate HTML files")]
        [Description("When a .html file exist in the same folder as an .md file it should automatically sync the .html file on save.")]
        [DefaultValue(true)]
        public bool GenerateHtmlFiles { get; set; } = true;
        
        [Category("HTML Generation")]
        [DisplayName("HTML file extension")]
        [Description("The file extension to use for the HTML file synced with the .md file in the HTML Generation process. Default is .html. Use .cshtml to generate html that can be incorporated with the Razor templating engine.")]
        [DefaultValue(".html")]
        public string HtmlFileExtension { get; set; } = ".html";

        [Category("HTML Generation")]
        [DisplayName("HTML Template file name")]
        [Description("The file name of a custom HTML template.")]
        [DefaultValue("md-template.html")]
        public string HtmlTemplateFileName { get; set; } = "md-template.html";

        // Style
        [Category("Style")]
        [DisplayName("Use system font in code blocks")]
        [Description("Determines if a system font should be used for code blocks")]
        [DefaultValue(true)]
        public bool CodeSystemFont { get; set; } = true;

        [Category("Style")]
        [DisplayName("Bold character")]
        [Description("Determines if bold should use double asterisk or underscore. Example: **bold text**")]
        [DefaultValue(EmphasisStyle.Asterisk)]
        public EmphasisStyle BoldStyle { get; set; } = EmphasisStyle.Asterisk;

        [Category("Style")]
        [DisplayName("Italic character")]
        [Description("Determines if italic should use single asterisk or underscore. Example: _italic text_")]
        [DefaultValue(EmphasisStyle.Asterisk)]
        public EmphasisStyle ItalicStyle { get; set; } = EmphasisStyle.Asterisk;

        // Preview window
        [Category("Preview Window")]
        [DisplayName("Enable Preview Window")]
        [Description("Determines if the preview window should be shown")]
        [DefaultValue(true)]
        public bool EnablePreviewWindow { get; set; } = true;

        [Category("Preview Window")]
        [DisplayName("Enable Sync Scrolling")]
        [Description("Determines if the preview should synchronize while scrolling the document")]
        [DefaultValue(true)]
        public bool EnablePreviewSyncNavigation { get; set; } = true;

        [Category("Preview Window")]
        [DisplayName("Show below the document")]
        [Description("Determines if the preview window should be located below the document or to the right. Reopen markdown document required.")]
        [DefaultValue(false)]
        public bool ShowPreviewWindowBelow { get; set; }

        [Category("Preview Window")]
        [DisplayName("Custom stylesheet name")]
        [Description("The file name of a custom stylesheet.")]
        [DefaultValue("md-styles.css")]
        public string CustomStylesheetFileName { get; set; } = "md-styles.css";

        [Category("Preview Window")]
        [DisplayName("Enable Preview Template")]
        [Description("Enable loading HTML Template in preview window.")]
        [DefaultValue(true)]
        public bool EnablePreviewTemplate { get; set; } = true;

        [Category("Preview Window")]
        [DefaultValue(600)]
        [Browsable(false)]
        public double PreviewWindowWidth { get; set; } = 600;

        [Category("Preview Window")]
        [DefaultValue(500)]
        [Browsable(false)]
        public double PreviewWindowHeight { get; set; } = 500;

        // Validation
        [Category("Validation")]
        [DisplayName("Enabled")]
        [Description("Enable validation to run on local link and image references.")]
        [DefaultValue(true)]
        public bool EnableValidation { get; set; } = true;
    }
}
