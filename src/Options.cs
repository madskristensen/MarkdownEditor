using System.ComponentModel;
using Microsoft.VisualStudio.Shell;

namespace MarkdownEditor
{
    public class Options : DialogPage
    {
        [Category("Preview Window")]
        [DisplayName("Enabled")]
        [Description("Determines if the preview window should be shown")]
        [DefaultValue(true)]
        public bool EnablePreviewWindow { get; set; } = true;

        [Category("Preview Window")]
        [DisplayName("Width")]
        [Description("The width of the preview window in pixels. Default is 600")]
        [DefaultValue(600)]
        public double PreviewWindowWidth { get; set; } = 600;
    }
}
