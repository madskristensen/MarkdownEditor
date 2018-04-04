using System;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using task = System.Threading.Tasks.Task;

internal static class Logger
{
    private static string _name;
    private static IVsOutputWindowPane _pane;
    private static IVsOutputWindow _output;

    public static void Initialize(IServiceProvider provider, string name)
    {
        _output = (IVsOutputWindow)provider.GetService(typeof(SVsOutputWindow));
        _name = name;
        // Ensure the pane before using it (otherwise first log will never come)
        EnsurePane();
    }

    public static void Log(object message, bool forceVisible = false)
    {
        try
        {
            if (EnsurePane())
            {
                _pane.OutputString(DateTime.Now + ": " + message + Environment.NewLine);
                if (forceVisible)
                {
                    _pane.Activate();
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.Write(ex);
        }
    }

    public static void LogOnError(Action callback)
    {
        try
        {
            callback.Invoke();
        }
        catch (Exception ex)
        {
            Log(ex);
        }
    }

    private static bool EnsurePane()
    {
        if (_pane == null && _output != null)
        {
            ThreadHelper.Generic.BeginInvoke(() =>
            {
                var guid = Guid.NewGuid();
                _output.CreatePane(ref guid, _name, 1, 1);
                _output.GetPane(ref guid, out _pane);
            });
        }

        return _pane != null;
    }
}