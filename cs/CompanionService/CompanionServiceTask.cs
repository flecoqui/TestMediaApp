using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.AppService;
using Windows.ApplicationModel.Background;
using Windows.Foundation.Collections;

namespace CompanionService
{
    public sealed class CompanionServiceTask : IBackgroundTask
    {
        BackgroundTaskDeferral serviceDeferral;
        AppServiceConnection connection;

        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            //Take a service deferral so the service isn't terminated
            serviceDeferral = taskInstance.GetDeferral();

            taskInstance.Canceled += OnTaskCanceled;


            var details = taskInstance.TriggerDetails as AppServiceTriggerDetails;
            connection = details.AppServiceConnection;

            //Listen for incoming app service requests
            connection.RequestReceived += OnRequestReceived;

            var inputs = new ValueSet();
            inputs.Add("type", "data");
            AppServiceResponse response = await connection.SendMessageAsync(inputs);
        }

        private void OnTaskCanceled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason)
        {
            if (serviceDeferral != null)
            {
                //Complete the service deferral
                serviceDeferral.Complete();
                serviceDeferral = null;
            }
        }

        async void OnRequestReceived(AppServiceConnection sender, AppServiceRequestReceivedEventArgs args)
        {
            //Get a deferral so we can use an awaitable API to respond to the message
            var messageDeferral = args.GetDeferral();

            try
            {
                var input = args.Request.Message;
                string message = (string)input["message"];

                //Create the response
                //var result = new ValueSet();
                //result.Add("result", randomNumberGenerator.Next(minValue, maxValue));

                //forward the message to foreground task
                //await args.Request.SendResponseAsync(result);
                var inputs = new ValueSet();
                inputs.Add("message", message);
                AppServiceResponse response = await connection.SendMessageAsync(inputs);
                //Create the response
                var result = new ValueSet();
                result.Add("result", response.Message);
                await args.Request.SendResponseAsync(result);

            }
            finally
            {
                //Complete the message deferral so the platform knows we're done responding
                messageDeferral.Complete();
            }
        }
    }

}
