//using System;
//using System.Runtime.InteropServices;
//using Microsoft.VisualStudio;
//using Microsoft.VisualStudio.OLE.Interop;
//using Microsoft.VisualStudio.Shell;
//using Microsoft.VisualStudio.Shell.Interop;
//using Microsoft.VisualStudio.TextManager.Interop;
//using IOleServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;


//namespace MarkdownEditor
//{
//    [Guid("ac40da2e-dba7-4853-8236-1f4720679a4e")]
//    public class EditorFactory : IVsEditorFactory
//    {
//        private MarkdownEditorPackage package;
//        private ServiceProvider serviceProvider;


//        public EditorFactory(MarkdownEditorPackage package)
//        {
//            this.package = package;
//        }

//        public virtual int SetSite(Microsoft.VisualStudio.OLE.Interop.IServiceProvider psp)
//        {
//            serviceProvider = new ServiceProvider(psp);
//            return VSConstants.S_OK;
//        }

//        public virtual object GetService(Type serviceType)
//        {
//            return serviceProvider.GetService(serviceType);
//        }

//        public virtual int CreateEditorInstance(
//                        uint createEditorFlags,
//                        string documentMoniker,
//                        string physicalView,
//                        IVsHierarchy hierarchy,
//                        uint itemid,
//                        System.IntPtr docDataExisting,
//                        out System.IntPtr docView,
//                        out System.IntPtr docData,
//                        out string editorCaption,
//                        out Guid commandUIGuid,
//                        out int createDocumentWindowFlags)
//        {
//            docView = IntPtr.Zero;
//            docData = IntPtr.Zero;
//            editorCaption = null;
//            commandUIGuid = new Guid("ac40da2e-dba7-4853-8236-1f4720679a4e");
//            createDocumentWindowFlags = 0;

//            IVsTextLines textLines = GetTextBuffer(docDataExisting);

//            // Assign docData IntPtr to either existing docData or the new text buffer
//            if (docDataExisting != IntPtr.Zero)
//            {
//                docData = docDataExisting;
//                Marshal.AddRef(docData);
//            }
//            else
//            {
//                docData = Marshal.GetIUnknownForObject(textLines);
//            }

//            try
//            {
//                docView = CreateDocumentView(physicalView, hierarchy, itemid, textLines, out editorCaption, out commandUIGuid);
//            }
//            finally
//            {
//                if (docView == IntPtr.Zero)
//                {
//                    if (docDataExisting != docData && docData != IntPtr.Zero)
//                    {
//                        // Cleanup the instance of the docData that we have addref'ed
//                        Marshal.Release(docData);
//                        docData = IntPtr.Zero;
//                    }
//                }
//            }

//            return VSConstants.S_OK;
//        }

//        private IVsTextLines GetTextBuffer(System.IntPtr docDataExisting)
//        {
//            IVsTextLines textLines;
//            if (docDataExisting == IntPtr.Zero)
//            {
//                // Create a new IVsTextLines buffer.
//                Type textLinesType = typeof(IVsTextLines);
//                Guid riid = textLinesType.GUID;
//                Guid clsid = typeof(VsTextBufferClass).GUID;
//                textLines = package.CreateInstance(ref clsid, ref riid, textLinesType) as IVsTextLines;

//                // set the buffer's site
//                ((IObjectWithSite)textLines).SetSite(serviceProvider.GetService(typeof(IOleServiceProvider)));
//            }
//            else
//            {
//                // Use the existing text buffer
//                Object dataObject = Marshal.GetObjectForIUnknown(docDataExisting);
//                textLines = dataObject as IVsTextLines;
//                if (textLines == null)
//                {
//                    // Try get the text buffer from textbuffer provider
//                    IVsTextBufferProvider textBufferProvider = dataObject as IVsTextBufferProvider;
//                    if (textBufferProvider != null)
//                    {
//                        textBufferProvider.GetTextBuffer(out textLines);
//                    }
//                }
//                if (textLines == null)
//                {
//                    // Unknown docData type then, so we have to force VS to close the other editor.
//                    Marshal.ThrowExceptionForHR(unchecked((int)0x80041FEA)); // VS_E_INCOMPATIBLEDOCDATA
//                }

//            }
//            return textLines;
//        }

//        private IntPtr CreateDocumentView(string physicalView, IVsHierarchy hierarchy, uint itemid, IVsTextLines textLines, out string editorCaption, out Guid cmdUI)
//        {
//            editorCaption = string.Empty;
//            cmdUI = Guid.Empty;

//            if (string.IsNullOrEmpty(physicalView))
//                return CreateCodeView(textLines, ref editorCaption, ref cmdUI);
//            else
//            {
//                Marshal.ThrowExceptionForHR(unchecked((int)0x80041FEB)); // VS_E_UNSUPPORTEDFORMAT
//                return IntPtr.Zero;
//            }
//        }

//        private IntPtr CreateCodeView(IVsTextLines textLines, ref string editorCaption, ref Guid cmdUI)
//        {
//            Type codeWindowType = typeof(IVsCodeWindow);
//            Guid riid = codeWindowType.GUID;
//            Guid clsid = typeof(VsCodeWindowClass).GUID;
//            IVsCodeWindow window = (IVsCodeWindow)package.CreateInstance(ref clsid, ref riid, codeWindowType);
//            window.SetBuffer(textLines);
//            window.SetBaseEditorCaption(null);
//            window.GetEditorCaption(READONLYSTATUS.ROSTATUS_Unknown, out editorCaption);
//            cmdUI = new Guid("{8B382828-6202-11d1-8870-0000F87579D2}"); // GUID_TextEditorFactory
//            return Marshal.GetIUnknownForObject(window);
//        }


//        public virtual int Close()
//        {
//            return VSConstants.S_OK;
//        }

//        public virtual int MapLogicalView(ref Guid logicalView, out string physicalView)
//        {
//            if (logicalView == VSConstants.LOGVIEWID_Primary)
//            {
//                physicalView = null;
//                return VSConstants.S_OK;
//            }
//            else
//            {
//                physicalView = null;
//                return VSConstants.E_NOTIMPL;
//            }
//        }
//    }
//}

