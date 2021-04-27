using System;
using Unity.Reflect.Utils;
using System.Windows.Forms;
using System.Linq;
using Unity.Reflect;

namespace PublisherSample
{
    public class Program : ILogReceiver
    {

        public static void Main(string[] args)
        {
            
            var guiMode = args.Contains("GUI");
            var publishType = args.Contains("sync") ? PublishType.ExportAndSync : PublishType.Export;

            if (guiMode)
            {
                // GUI mode
                // -------------

                // Preparing the GUI
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                // Opening the form
                Application.Run(new SampleGUI(publishType));
            }
            else
            {
                // Console mode
                // -------------

                // Subscribe to the Reflect logger to display the logs in the console.
                // Note : you can also use the FileLogReceiver class for easy file logging.
                Logger.AddReceiver(new Program());

                // Run the publisher sample code
                PublisherSample.Run(publishType);

                // Wait for user input to exit
                Console.ReadLine();
            }
        }

        public void LogReceived(Logger.Level level, string msg)
        {
            Console.WriteLine(msg);
        }
    }
}
