using System;
using Unity.Reflect.UI;
using Unity.Reflect;
using System.IO;

namespace ConfigFileCreator
{
    class Program
    {
        public static void Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.WriteLine("Please provide a path for the file to write.");
                return;
            }

            var filePath = args[0];
            var settings = WindowFactory.ShowPublisherSettings("ConfigFileCreator", new Version(), PublishType.Export);

            settings.WriteToFile(filePath);
        }
    }
}
