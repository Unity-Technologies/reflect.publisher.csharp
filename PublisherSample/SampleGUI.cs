using System;
using System.Windows.Forms;
using Unity.Reflect.Utils;

namespace Unity.Reflect.PublisherSample
{
    public partial class SampleGUI : Form, ILogReceiver
    {
        private PublishType m_PublishType;

        public SampleGUI(PublishType publishType)
        {
            m_PublishType = publishType;
            InitializeComponent();

            // Subscribe to the Reflect logger to display the logs in the console.
            // Note : you can also use the FileLogReceiver class for easy file logging.
            Logger.AddReceiver(this);
        }

        void ILogReceiver.LogReceived(Logger.Level level, string msg)
        {
            if (InvokeRequired)
                Invoke(new Action(() => textBox1.Text += msg + Environment.NewLine));
            else
                textBox1.Text += msg + Environment.NewLine;
        }

        private void SampleGUI_Shown(object sender, EventArgs e)
        {
            // Run the publisher sample code
            PublisherSample.Run(m_PublishType);
        }
    }
}
