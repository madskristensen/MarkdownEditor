using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace MarkdownEditor
{
    // TODO: Refactor this so code can be shared between the classes

    [Export(typeof(IWpfTextViewMarginProvider))]
    [Name("MarginRightFactory")]
    [Order(After = PredefinedMarginNames.RightControl)]
    [MarginContainer(PredefinedMarginNames.Right)]
    [ContentType(MarkdownLanguage.LanguageName)]
    [TextViewRole(PredefinedTextViewRoles.Debuggable)] // This is to prevent the margin from loading in the diff view
    public class BrowserMarginRightProvider : IWpfTextViewMarginProvider
    {
        [Import]
        public ITextDocumentFactoryService TextDocumentFactoryService { get; set; }

        public IWpfTextViewMargin CreateMargin(IWpfTextViewHost wpfTextViewHost, IWpfTextViewMargin marginContainer)
        {
            if (!MarkdownEditorPackage.Options.EnablePreviewWindow || MarkdownEditorPackage.Options.ShowPreviewWindowBelow)
                return null;

            ITextDocument document;

            if (!TextDocumentFactoryService.TryGetTextDocument(wpfTextViewHost.TextView.TextDataModel.DocumentBuffer, out document))
                return null;

            return wpfTextViewHost.TextView.Properties.GetOrCreateSingletonProperty(() => new BrowserMargin(wpfTextViewHost.TextView, document));
        }
    }
    
    [Export(typeof(IWpfTextViewMarginProvider))]
    [Name("MarginBottomFactory")]
    [Order(After = PredefinedMarginNames.BottomControl)]
    [MarginContainer(PredefinedMarginNames.Bottom)]
    [ContentType(MarkdownLanguage.LanguageName)]
    [TextViewRole(PredefinedTextViewRoles.Debuggable)] // This is to prevent the margin from loading in the diff view
    public class BrowserMarginBottomProvider : IWpfTextViewMarginProvider
    {
        [Import]
        public ITextDocumentFactoryService TextDocumentFactoryService { get; set; }

        public IWpfTextViewMargin CreateMargin(IWpfTextViewHost wpfTextViewHost, IWpfTextViewMargin marginContainer)
        {
            if (!MarkdownEditorPackage.Options.EnablePreviewWindow || !MarkdownEditorPackage.Options.ShowPreviewWindowBelow)
                return null;

            ITextDocument document;

            if (!TextDocumentFactoryService.TryGetTextDocument(wpfTextViewHost.TextView.TextDataModel.DocumentBuffer, out document))
                return null;

            return wpfTextViewHost.TextView.Properties.GetOrCreateSingletonProperty(() => new BrowserMargin(wpfTextViewHost.TextView, document));
        }
    }

    [Export(typeof(IWpfTextViewMarginProvider))]
    [Name("LiveSyncFactory")]
    [Order(Before = PredefinedMarginNames.HorizontalScrollBarContainer)]
    [MarginContainer(PredefinedMarginNames.BottomRightCorner)]
    [ContentType(MarkdownLanguage.LanguageName)]
    [TextViewRole(PredefinedTextViewRoles.Debuggable)] // This is to prevent the margin from loading in the diff view
    public class LiveSyncMarginBottomProvider : IWpfTextViewMarginProvider
    {
        public IWpfTextViewMargin CreateMargin(IWpfTextViewHost wpfTextViewHost, IWpfTextViewMargin marginContainer)
        {
            if (!MarkdownEditorPackage.Options.EnablePreviewWindow)
                return null;

            return wpfTextViewHost.TextView.Properties.GetOrCreateSingletonProperty(() => new LiveSyncMargin(wpfTextViewHost.TextView));
        }
    }
}
