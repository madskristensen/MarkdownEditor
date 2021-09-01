using System;
using System.Globalization;
using Microsoft.VisualStudio.Shell;

namespace MarkdownEditor
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public class ProvideBraceCompletionAttribute : RegistrationAttribute
    {
        private string languageName;
        public ProvideBraceCompletionAttribute(string languageName)
        {
            this.languageName = languageName;
        }

        public override void Register(RegistrationContext context)
        {
            string keyName = string.Format(CultureInfo.InvariantCulture, "{0}\\{1}\\{2}", "Languages", "Language Services", languageName);
            using (Key langKey = context.CreateKey(keyName))
            {
                langKey.SetValue("ShowBraceCompletion", 1);
            }
        }

        public override void Unregister(RegistrationContext context)
        {
            string keyName = string.Format(CultureInfo.InvariantCulture, "{0}\\{1}\\{2}", "Languages", "Language Services", languageName);
            context.RemoveKey(keyName);
        }
    }
}
