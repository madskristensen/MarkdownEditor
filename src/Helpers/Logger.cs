using System;
using Microsoft.VisualStudio.Shell.Interop;

public static class Logger
{
    private static string _name;
    private static IVsOutputWindowPane _pane;
    private static IVsOutputWindow _output;

    public static void Initialize(IServiceProvider provider, string name)
    {
        _output = (IVsOutputWindow)provider.GetService(typeof(SVsOutputWindow));
        _name = name;
    }

    public static void Log(object message)
    {
        try
        {
            if (EnsurePane())
            {
                _pane.OutputString(DateTime.Now.ToString() + ": " + message + Environment.NewLine);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.Write(ex);
        }
    }

    private static bool EnsurePane()
    {
        if (_pane == null)
        {
            Guid guid = Guid.NewGuid();
            _output.CreatePane(ref guid, _name, 1, 1);
            _output.GetPane(ref guid, out _pane);
        }

        return _pane != null;
    }
}