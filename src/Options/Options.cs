using System.ComponentModel;
using Microsoft.VisualStudio.Shell;

namespace MarkdownEditor
{
    public class Options : DialogPage
    {
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

        [Category("Preview Window")]
        [DisplayName("Enabled")]
        [Description("Determines if the preview window should be shown")]
        [DefaultValue(true)]
        public bool EnablePreviewWindow { get; set; } = true;

        [Category("Preview Window")]
        [DisplayName("Width")]
        [Description("The width of the preview window in pixels. Default is 600")]
        [DefaultValue(600)]
        [Browsable(false)]
        public double PreviewWindowWidth { get; set; } = 600;
    }
}
