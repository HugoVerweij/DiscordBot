using System;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace Honata.Modules.Helpers
{
    public static class SaveHelper
    {
        public async static Task SaveTypeof(Type _type, object _object, string _path)
        {
            try
            {
                // Create the doc.
                XDocument doc = new XDocument();

                // Assign the writer.
                using (XmlWriter writer = doc.CreateWriter())
                {
                    // Create the serializer and serialize the data.
                    XmlSerializer serializer = new XmlSerializer(_type);
                    serializer.Serialize(writer, _object);
                }

                // Save the document.
                doc.Save(_path);
            }
            catch (Exception e)
            {
                // TODO: Throw exception.
                //Console.WriteLine(e);
            }

            await Task.CompletedTask;
        }
    }
}
