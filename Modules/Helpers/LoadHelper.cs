using Honata.Models.Servers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace Honata.Modules.Helpers
{
    public static class LoadHelper
    {
        public static async Task<object> LoadTypeDataAsync(Type _type, string _path)
        {
            // Set the result.
            object result = null;

            // Force the task to run on a new thread.
            await Task.Run(() =>
            {
                try
                {
                    // Check if the file exists.
                    if (File.Exists(_path))
                    {
                        // Create a new serializer.
                        XmlSerializer ser = new XmlSerializer(typeof(List<>).MakeGenericType(_type));
                        
                        // Create a new reader.
                        using XmlReader reader = XmlReader.Create(_path);

                        // Return the result.
                        result = ser.Deserialize(reader);
                    }
                }
                catch (Exception e)
                {
                    // TODO: Throw exception.
                    Console.WriteLine(e);
                }
            });

            // Return the result.
            return result;
        }
    }
}
