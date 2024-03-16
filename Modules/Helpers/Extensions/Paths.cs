using System;
using System.IO;

namespace Honata.Modules.Helpers
{
    public static class Paths
    {
        // Root.
        public static string ExeDir = AppDomain.CurrentDomain.BaseDirectory;
        public static string XmlDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "XML");

        // Xml.
        public static string Servers = Path.Combine(XmlDir, "Servers.xml");
        public static string Users = Path.Combine(XmlDir, "Users.xml");
    }
}
