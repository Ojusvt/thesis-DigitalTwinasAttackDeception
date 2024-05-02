// Default URL for triggering event grid function in the local environment.
// http://localhost:7071/runtime/webhooks/EventGrid?functionName={functionname}
using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.EventGrid;
using Microsoft.Extensions.Logging;
using Azure.Messaging.EventGrid;

//TO interact with Azure ADT
using Azure.DigitalTwins.Core;
using Azure.Identity;
using System.Net.Http;
using Azure.Core.Pipeline;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using Azure;
using System.Numerics;
using static Microsoft.ApplicationInsights.MetricDimensionNames.TelemetryContext;
using Microsoft.Azure.Functions.Worker;

namespace LevelSensorIngestFunction
{
    public static class Function1
    {
        // ADT Instance
        private static readonly string adtInstanceUrl = Environment.GetEnvironmentVariable("ADT_SERVICE_URL");
        private static readonly HttpClient httpClient = new HttpClient();

        [FunctionName("IOTHubToLIT101ADTFunction")]
        public static async Task Run([EventGridTrigger] EventGridEvent eventGridEvent, ILogger log)
        {
            log.LogInformation(eventGridEvent.Data.ToString());

            if (adtInstanceUrl == null) log.LogError("Application setting \"ADT_SERVICE_URL\" not set");

            try
            {
                //Managed Identity Credentials
                var cred = new DefaultAzureCredential();

                //Instantiate ADT Client
                var adtClient = new DigitalTwinsClient(new Uri(adtInstanceUrl), cred);

                // Log successful connection creation
                log.LogInformation($"LIT101 ADT client connection created!");

                //if we receive data
                if (eventGridEvent != null && eventGridEvent.Data != null)
                {
                    //Log the data
                    log.LogInformation(eventGridEvent.Data.ToString());

                    //Covert to json
                    JObject LIT101Message = (JObject)JsonConvert.DeserializeObject(eventGridEvent.Data.ToString());

                    //Get device data from object
                    string LIT101Id = (String)LIT101Message["systemProperties"]["iothub-connection-device-id"];

                    double levelm = (double)LIT101Message["body"]["levelm"];


                    //Log the telemetry
                    log.LogInformation($"Device: {LIT101Id} levelm: {levelm}");

                    var updateLIT101TwinData = new JsonPatchDocument();
                    updateLIT101TwinData.AppendReplace("/levelm", levelm);


                    await adtClient.UpdateDigitalTwinAsync(LIT101Id, updateLIT101TwinData).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                log.LogError($"Error in FIT201 Ingest Function: {ex.Message}");
            }
        }
    }

}