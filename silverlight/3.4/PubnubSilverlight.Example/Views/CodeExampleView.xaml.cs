using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Navigation;
using System.Diagnostics;
using System.ComponentModel;
using System.IO;
using System.Threading;
using Microsoft.Silverlight.Testing;
using PubNubMessaging.Core;

namespace PubNub_Messaging
{
    public partial class CodeExampleView : Page
    {

        #region "Properties and Members"

        static public Pubnub pubnub;

        static public bool deliveryStatus = false;
        static public string channel = "";
        static bool ssl = false;
        static string secretKey = "";
        static string cipherKey = "";
        static string uuid = "";
        static bool resumeOnReconnect = false;

        static public bool enableSSL = false;
        static public string cipheryKey = string.Empty;

        static int subscribeTimeoutInSeconds = 0;
        static int operationTimeoutInSeconds = 0;
        static int networkMaxRetries = 0;
        static int networkRetryIntervalInSeconds = 0;
        static int heartbeatIntervalInSeconds = 0;
       
        #endregion

        public CodeExampleView()
        {
            InitializeComponent();

            Console.Container = ConsoleContainer;

            ssl = chkSSL.IsChecked.Value;
            secretKey = txtSecret.Text;
            cipherKey = txtCipher.Text;
            uuid = txtUUID.Text;
            resumeOnReconnect = chkResumeOnReconnect.IsChecked.Value;

            Int32.TryParse(txtSubscribeTimeout.Text, out subscribeTimeoutInSeconds);
            subscribeTimeoutInSeconds = (subscribeTimeoutInSeconds <= 0) ? 310 : subscribeTimeoutInSeconds;

            Int32.TryParse(txtNonSubscribeTimeout.Text, out operationTimeoutInSeconds);
            operationTimeoutInSeconds = (operationTimeoutInSeconds <= 0) ? 15 : operationTimeoutInSeconds;

            Int32.TryParse(txtNetworkMaxRetries.Text, out networkMaxRetries);
            networkMaxRetries = (networkMaxRetries <= 0) ? 50 : networkMaxRetries;

            Int32.TryParse(txtRetryInterval.Text, out networkRetryIntervalInSeconds);
            networkRetryIntervalInSeconds = (networkRetryIntervalInSeconds <= 0) ? 10 : networkRetryIntervalInSeconds;

            Int32.TryParse(txtHeartbeatInterval.Text, out heartbeatIntervalInSeconds);
            heartbeatIntervalInSeconds = (heartbeatIntervalInSeconds <= 0) ? 10 : heartbeatIntervalInSeconds;

            pubnub = new Pubnub("demo", "demo", secretKey, cipheryKey, ssl);

        }

        private void Subscribe_Click(object sender, RoutedEventArgs e)
        {
            channel = txtChannel.Text;
            Console.WriteLine("Running subscribe()");
            pubnub.Subscribe<string>(channel, DisplayUserCallbackMessage, DisplayConnectCallbackMessage);
        }

        private void Publish_Click(object sender, RoutedEventArgs e)
        {
            channel = txtChannel.Text;
            Console.WriteLine("Running publish()");

            PublishMessageDialog publishView = new PublishMessageDialog();

            publishView.Show();

            publishView.Closed += (obj, args) => 
            {
                if (publishView.DialogResult == true && publishView.Message.Text.Length > 0)
                {
                    string publishedMessage = publishView.Message.Text;
                    pubnub.Publish<string>(channel, publishedMessage, DisplayUserCallbackMessage);
                }
            };
        }

        private void Presence_Click(object sender, RoutedEventArgs e)
        {
            channel = txtChannel.Text;
            Console.WriteLine("Running presence()");
            pubnub.Presence<string>(channel, DisplayUserCallbackMessage, DisplayConnectCallbackMessage);
        }

        private void History_Click(object sender, RoutedEventArgs e)
        {
            channel = txtChannel.Text;
            Console.WriteLine("Running detailed history()");
            pubnub.DetailedHistory<string>(channel, 10, DisplayUserCallbackMessage);
        }

        private void HereNow_Click(object sender, RoutedEventArgs e)
        {
            channel = txtChannel.Text;
            Console.WriteLine("Running Here_Now()");
            pubnub.HereNow<string>(channel, DisplayUserCallbackMessage);
        }

        private void Unsubscribe_Click(object sender, RoutedEventArgs e)
        {
            channel = txtChannel.Text;
            Console.WriteLine("Running unsubscribe()");
            pubnub.Unsubscribe<string>(channel, DisplayUserCallbackMessage, DisplayUserCallbackMessage, DisplayDisconnectCallbackMessage);
        }

        private void PresenceUnsubscrib_Click(object sender, RoutedEventArgs e)
        {
            channel = txtChannel.Text;
            Console.WriteLine("Running presence-unsubscribe()");
            pubnub.PresenceUnsubscribe<string>(channel, DisplayUserCallbackMessage, DisplayConnectCallbackMessage, DisplayDisconnectCallbackMessage);
        }

        private void Time_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("Running time()");
            pubnub.Time<string>(DisplayUserCallbackMessage);
        }

        static void DisplayUserCallbackMessage(string result)
        {
            Console.WriteLine(result);
        }

        static void DisplayConnectCallbackMessage(string result)
        {
            Console.WriteLine(result);
        }

        static void DisplayDisconnectCallbackMessage(string result)
        {
            Console.WriteLine(result);
        }

        private void btnDisconnectRetry_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("Running Disconnect/auto-Reconnect Subscriber Request Connection");
            pubnub.TerminateCurrentSubscriberRequest();
        }

        private void btnDisableNetwork_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("");
            Console.WriteLine("Disabling Network Connection (no internet)");
            Console.WriteLine("Initiating Simulation of Internet non-availability");
            Console.WriteLine("Until \"Enable Network\" is selected, no operations will occur");
            Console.WriteLine("NOTE: Publish from other pubnub clients can occur and those will be ");
            Console.WriteLine("      captured upon \"Enable Network\" is selected, provided resume on reconnect is enabled.");
            

            pubnub.EnableSimulateNetworkFailForTestingOnly();
        }

        private void btnEnableNetwork_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("");
            Console.WriteLine("Enabling Network Connection (yes internet)");
            pubnub.DisableSimulateNetworkFailForTestingOnly();
        }

    }

    #region "Console View"

    public class Console
    {
        internal static TextBlock Container { get; set; }

        public static void WriteLine(string format)
        {
            Container.Dispatcher.BeginInvoke(() =>
            {
                if (Container != null)
                {
                    if (Container.Text == null)
                    {
                        Container.Text = "";
                    }
                    Container.Text += format + "\r\n";
                }
            });
        }

        public static void Clear()
        {
            if (Container != null)
            {
                Container.Text = string.Empty;
            }
        }

    }

    #endregion

}
