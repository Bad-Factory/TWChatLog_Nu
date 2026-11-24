using System;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Resources;

namespace TWChatLog_Nu.Properties
{
    internal class Resources
    {
        private static ResourceManager resourceMan;
        private static CultureInfo resourceCulture;

        internal Resources()
        {
        }

        internal static ResourceManager ResourceManager
        {
            get
            {
                if (object.ReferenceEquals(resourceMan, null))
                {
                    ResourceManager temp = new ResourceManager("TWChatLog_Nu.Properties.Resources", typeof(Resources).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }

        internal static CultureInfo Culture
        {
            get { return resourceCulture; }
            set { resourceCulture = value; }
        }

        internal static Icon nu
        {
            get
            {
                object obj = ResourceManager.GetObject("nu", resourceCulture);
                if (obj is Icon)
                {
                    return (Icon)obj;
                }

                Assembly asm = typeof(Resources).Assembly;
                using (Stream s = asm.GetManifestResourceStream("TWChatLog_Nu.nu.ico"))
                {
                    if (s != null)
                    {
                        return new Icon(s);
                    }
                }
                return null;
            }
        }

        internal static byte[] Notification
        {
            get
            {
                object obj = ResourceManager.GetObject("Notification", resourceCulture);
                if (obj is byte[])
                {
                    return (byte[])obj;
                }

                Assembly asm = typeof(Resources).Assembly;
                using (Stream s = asm.GetManifestResourceStream("TWChatLog_Nu.Notification.mp3"))
                {
                    if (s == null) return null;
                    using (var ms = new MemoryStream())
                    {
                        s.CopyTo(ms);
                        return ms.ToArray();
                    }
                }
            }
        }
    }
}