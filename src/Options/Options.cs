using System.ComponentModel;
using Microsoft.VisualStudio.Shell;

namespace MarkdownEditor
{
    public class Options : DialogPage
    {
        // General
        [Category("General")]
        [DisplayName("Enable outlining")]
        [Description("Determines if outlining (code folding) should be enabled for multiline HTML and code blocks")]
        [DefaultValue(true)]
        public bool EnableOutlining { get; set; } = true;

        [Category("General")]
        [DisplayName("Enable typethrough")]
        [Description("Determines if completion of braces, * and _ is enabled")]
        [DefaultValue(true)]
        public bool EnableTypeThrough { get; set; } = true;

        [Category("General")]
        [DisplayName("Enable hotkeys")]
        [Description("Enables CTRL+B for bold and CTRL+I for italic.")]
        [DefaultValue(true)]
        public bool EnableHotKeys { get; set; } = true;

        [Category("General")]
        [DisplayName("Enable smart indent")]
        [Description("Automatically adds new list items on Enter.")]
        [DefaultValue(true)]
        public bool EnableSmartIndent { get; set; } = true;

        // Style
        [Category("Style")]
        [DisplayName("Bold character")]
        [Description("Determines if bold should use double asterisk or underscore. Example: **bold text**")]
        [DefaultValue(EmphasisStyle.Asterisk)]
        public EmphasisStyle BoldStyle { get; set; } = EmphasisStyle.Asterisk;

        [Category("Style")]
        [DisplayName("Italic character")]
        [Description("Determines if italic should use single asterisk or underscore. Example: _italic text_")]
        [DefaultValue(EmphasisStyle.Underscore)]
        public EmphasisStyle ItalicStyle { get; set; } = EmphasisStyle.Underscore;

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
