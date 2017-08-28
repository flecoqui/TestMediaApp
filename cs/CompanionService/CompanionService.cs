//*********************************************************
//
// Copyright (c) Microsoft. All rights reserved.
// This code is licensed under the MIT License (MIT).
// THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
// IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
// PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.
//
//*********************************************************
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
        static AppServiceConnection localConnection;
        AppServiceConnection remoteConnection;

        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            if (taskInstance != null)
            {
                //Take a service deferral so the service isn't terminated
                serviceDeferral = taskInstance.GetDeferral();

                taskInstance.Canceled += OnTaskCanceled;


                var details = taskInstance.TriggerDetails as AppServiceTriggerDetails;
                if (details != null)
                {
                    if (details.IsRemoteSystemConnection == true)
                    {
                        remoteConnection = details.AppServiceConnection;
                        remoteConnection.RequestReceived += OnRequestReceived;
                        remoteConnection.ServiceClosed += RemoteConnection_ServiceClosed;
                    }
                    else
                    {
                        localConnection = details.AppServiceConnection;
                        localConnection.RequestReceived += OnRequestReceived;
                        localConnection.ServiceClosed += LocalConnection_ServiceClosed;

                        // Send a notification to the foreground task
                        var inputs = new ValueSet();
                        inputs.Add(CompanionServiceMessage.ATT_TYPE, CompanionServiceMessage.TYPE_INIT);
                        AppServiceResponse response = await localConnection.SendMessageAsync(inputs);
                    }

                }
            }
        }

        private void LocalConnection_ServiceClosed(AppServiceConnection sender, AppServiceClosedEventArgs args)
        {
            if(localConnection!=null)
            {
                localConnection.RequestReceived -= OnRequestReceived;
                localConnection.ServiceClosed -= LocalConnection_ServiceClosed;
                localConnection = null;
            }
        }
        private void RemoteConnection_ServiceClosed(AppServiceConnection sender, AppServiceClosedEventArgs args)
        {
            if (remoteConnection != null)
            {
                remoteConnection.RequestReceived -= OnRequestReceived;
                remoteConnection.ServiceClosed -= RemoteConnection_ServiceClosed;
                remoteConnection = null;
            }
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
                if ((args.Request != null) && (args.Request.Message != null))
                {
                    var inputs = args.Request.Message;
                    if (inputs.ContainsKey(CompanionServiceMessage.ATT_TYPE))
                    {
                        string s = (string)inputs[CompanionServiceMessage.ATT_TYPE];
                        if (string.Equals(s, CompanionServiceMessage.TYPE_DATA))
                        {
                            if ((inputs.ContainsKey(CompanionServiceMessage.ATT_SOURCEID)) &&
                            (inputs.ContainsKey(CompanionServiceMessage.ATT_SOURCEID)) &&
                            (inputs.ContainsKey(CompanionServiceMessage.ATT_SOURCEIP)) &&
                            (inputs.ContainsKey(CompanionServiceMessage.ATT_SOURCENAME)) &&
                            (inputs.ContainsKey(CompanionServiceMessage.ATT_SOURCEKIND)) &&
                            (inputs.ContainsKey(CompanionServiceMessage.ATT_MESSAGE)))
                            {
                                string id = (string)inputs[CompanionServiceMessage.ATT_SOURCEID];
                                string name = (string)inputs[CompanionServiceMessage.ATT_SOURCENAME];
                                string ip = (string)inputs[CompanionServiceMessage.ATT_SOURCEIP];
                                string kind = (string)inputs[CompanionServiceMessage.ATT_SOURCEKIND];
                                string message = (string)inputs[CompanionServiceMessage.ATT_MESSAGE];
                                if (localConnection != null)
                                {
                                    AppServiceResponse response = await localConnection.SendMessageAsync(inputs);
                                }

                            }
                        }
                        else if (string.Equals(s, CompanionServiceMessage.TYPE_INIT))
                        {
                            // Background task started
                        }
                    }
                    //Create the response
                    var result = new ValueSet();
                    result.Add(CompanionServiceMessage.ATT_TYPE, CompanionServiceMessage.TYPE_RESULT);
                    result.Add(CompanionServiceMessage.ATT_RESULT, CompanionServiceMessage.VAL_RESULT_OK);
                    await args.Request.SendResponseAsync(result);
                }
            }
            finally
            {
                //Complete the message deferral so the platform knows we're done responding
                messageDeferral.Complete();
            }
        }
    }

}
