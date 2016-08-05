using System;
using Microsoft.VisualStudio.Shell;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
internal sealed class ProvideFileExtensionMapping : RegistrationAttribute
{
    private readonly string _name, _id, _editorGuid, _logViewGuid, _package;
    private readonly int _sortPriority;

    public ProvideFileExtensionMapping(string id, string name, Type editorGuid, Type logViewGuid, string package, int sortPriority)
    {
        _id = id;
        _name = name;
        _editorGuid = ((Type)editorGuid).GUID.ToString("B");
        _logViewGuid = ((Type)logViewGuid).GUID.ToString("B");
        _package = package;
        _sortPriority = sortPriority;
    }

    public override void Register(RegistrationContext context)
    {
        using (Key mappingKey = context.CreateKey("FileExtensionMapping\\" + _id))
        {
            mappingKey.SetValue("", _name);
            mappingKey.SetValue("DisplayName", _name);
            mappingKey.SetValue("EditorGuid", _editorGuid);
            mappingKey.SetValue("LogViewID", _logViewGuid);
            mappingKey.SetValue("Package", _package);
            mappingKey.SetValue("SortPriority", _sortPriority);
        }
    }

    public override void Unregister(RegistrationContext context)
    {
    }
}