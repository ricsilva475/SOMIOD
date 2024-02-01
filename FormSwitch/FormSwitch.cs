using System;
using System.Diagnostics;
using System.Net;
using System.Text.Json;
using System.Windows.Forms;
using FormSwitch.Models;
using FormSwitch.Properties;
using RestSharp;
using uPLibrary.Networking.M2Mqtt;
using static FormSwitch.Properties.Settings;
using Application = FormLightBulb.Models.Application;

namespace FormSwitch
{
    public partial class FormSwitch : Form
    {
        #region Constants

        private static readonly string ApiBaseUri = Default.ApiBaseUri;
        private static readonly string ApplicationName = Default.ApplicationName;
        private static readonly string ContainerName = Default.ContainerName;
        private static readonly string ContainerToSendData = Default.ContainerToSendData;

        #endregion

        private readonly RestClient _restClient = new RestClient(ApiBaseUri);

        public FormSwitch()
        {
            InitializeComponent();
        }


        #region API Calls

        private void CreateApplication(string applicationName)
        {
            var app = new Application(applicationName);

            var request = new RestRequest("api/somiod", Method.Post);
            request.RequestFormat = DataFormat.Xml;
            request.AddBody(app);

            var response = _restClient.Execute(request);
            

        }

        private void CreateData(string appName, string containerToSendData, string content)
        {
            var data = new Data(content);

            var request = new RestRequest($"api/somiod/{appName}/{containerToSendData}/data", Method.Post);
            request.RequestFormat = DataFormat.Xml;
            request.AddBody(data); // Adiciona o objeto Data ao corpo da solicitação

            var response = _restClient.Execute(request);

        }

        #endregion

        private void btnOn_Click(object sender, EventArgs e)
        {
            CreateData(ApplicationName, ContainerToSendData, "OPEN");
        }

        private void btnOff_Click(object sender, EventArgs e)
        {
            CreateData(ApplicationName, ContainerToSendData, "CLOSED");
        }

        private void FormSwitch_Shown(object sender, EventArgs e)
        {
            CreateApplication(ApplicationName);
        }

        private void FormSwitch_Load(object sender, EventArgs e)
        {

        }
    }
}
