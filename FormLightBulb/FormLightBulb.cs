using System;
using System.ComponentModel;
using System.Data.SqlClient;
using System.IO;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Windows.Forms;
using System.Xml.Serialization;
using FormLightBulb.Models;
using FormLightBulb.Properties;
using RestSharp;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;
using static FormLightBulb.Properties.Settings;
using Application = FormLightBulb.Models.Application;
using Container = FormLightBulb.Models.Container;

namespace FormLightBulb
{
    public partial class FormLightBulb : Form
    {
        #region Constants

        private static readonly string BrokerIp = Default.BrokerIp;
        private static readonly string ApiBaseUri = Default.ApiBaseUri;
        private static readonly HttpStatusCode CustomApiError = (HttpStatusCode)Default.CustomApiError;
        private static readonly string ApplicationName = Default.ApplicationName;
        private static readonly string ContainerName = Default.ContainerName;
        private static readonly string SubscriptionName = Default.SubscriptionName;
        private static readonly string Subscription_Event = Default.Subscription_Event;
        private static readonly string Endpoint = Default.Endpoint;
        private static readonly string[] Topic = { Default.Topic };

        #endregion

        private MqttClient _mClient;
        private readonly RestClient _restClient = new RestClient(ApiBaseUri);
        private bool _turnDoorOpen;

        public FormLightBulb()
        {
            InitializeComponent();
        }

        #region Helpers

        private void UpdateDoorState()
        {
            // Ajustar o estado da porta e a imagem do PictureBox
            pbDoor.Image = _turnDoorOpen ? Resources.porta_aberta : Resources.porta_fechada;
        }
        private void client_MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs args)
        {

            string message = Encoding.UTF8.GetString(args.Message);

            if (message.ToLower() == "open")
            {
                _turnDoorOpen = true;
                Console.WriteLine("Porta aberta. _turnDoorOpen definido como true.");
            }
            else
            {
                _turnDoorOpen = false;
                Console.WriteLine("Porta fechada. _turnDoorOpen definido como false.");
            }

            // Atualizar o estado da porta
            UpdateDoorState();

        }
        #endregion

        #region Message Broker
        private void ConnectToBroker()
        {
            _mClient = new MqttClient(BrokerIp);
            _mClient.Connect(Guid.NewGuid().ToString());

            if (!_mClient.IsConnected)
            {
                MessageBox.Show("Could not connect to the message broker", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Close();
            }
        }

        private void SubscribeToTopics()
        {
            byte[] qosLevels = { MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE };
            _mClient.Subscribe(Topic, qosLevels);
            _mClient.MqttMsgPublishReceived += client_MqttMsgPublishReceived;
        }

        #endregion

        #region API Calls

        private void CreateApplication(string applicationName)
        {

            var app = new Application(applicationName);

            var request = new RestRequest("api/somiod", Method.Post);
            request.RequestFormat = DataFormat.Xml;
            request.AddBody(app);

            var response = _restClient.Execute(request);

        }

        private void CreateContainer(string containerName, string applicationName)
        { 
            var container = new Container(containerName);

            var request = new RestRequest($"api/somiod/{applicationName}/container", Method.Post);
            request.RequestFormat = DataFormat.Xml;
            request.AddBody(container);

            var response = _restClient.Execute(request);

            
        }



        private void CreateSubscription(string subscriptionName, string containerName, string applicationName, string subscription_event, string endpoint)
        {
            

            var sub = new Subscription(subscriptionName, subscription_event, endpoint);

            var request = new RestRequest($"api/somiod/{applicationName}/{containerName}/subscription", Method.Post);
            request.RequestFormat = DataFormat.Xml;
            request.AddBody(sub);

            var response = _restClient.Execute(request);

           
        }


        #endregion

        private void FormLightBulb_Shown(object sender, EventArgs e)
        {
            CreateApplication(ApplicationName);
            CreateContainer(ContainerName, ApplicationName);
            CreateSubscription(SubscriptionName, ContainerName, ApplicationName, Subscription_Event, Endpoint);
            ConnectToBroker();
            SubscribeToTopics();
        }

        private void FormLightBulb_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_mClient.IsConnected)
            {
                _mClient.Unsubscribe(Topic);
                _mClient.Disconnect();
            }
        }

        private void FormLightBulb_Load(object sender, EventArgs e)
        {

        }

        private void pbLightbulb_Click(object sender, EventArgs e)
        {

        }


    }
}