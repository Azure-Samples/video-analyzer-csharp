// -----------------------------------------------------------------------
//  <copyright company="Microsoft Corporation">
//       Copyright (C) Microsoft Corporation. All rights reserved.
//  </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Rest.Azure.Authentication;
using Microsoft.Azure.Management.VideoAnalyzer;
using Microsoft.Azure.Management.VideoAnalyzer.Models;
using PipelineResponseException = Microsoft.Azure.Management.VideoAnalyzer.Models.ErrorResponseException;

namespace PrivateCameraPipelineSampleCode
{
    class Program
    {
        // Account details
        private const string SubscriptionId = "<Provide subscription id>";
        private const string ResourceGroupName = "<Provide resource group name>";
        private const string AccountName = "<Provide ava account name>";
        private const string TenantId = "<Provide tenant id>";
        private const string ClientId = "<Provide app registration client id>";
        private const string Secret = "<Provide app registration client secret>";
        private static Uri AuthenticationEndpoint = new Uri("<Provide authentication end point>");
        private static Uri ArmEndPoint = new Uri("<Provide arm end point here>");
        private static Uri TokenAudience = new Uri("<Provide token audience>");

        // private camera ingestion parameters for pipeline setup
        private const string PrivateCameraIngestionTunnelingDeviceId = "<Provide device Id>";
        private const string iotHubNameForPrivateCameraIngestion = "<Provide IOT hub name>";
        private const string PrivateCameraIngestionSourceRTSPURL = "<Provide RTSP source url>";
        private const string PrivateCameraIngestionSourceRTSPUserName = "<Provide RTSP source username>";
        private const string PrivateCameraIngestionSourceRTSPPassword = "<Provide RTSP source password>";

        private const string PrivateCameraIngestionTopologyName = "PrivIngestionTopology-1";
        private const string PrivateCameraIngestionPipelineName = "PrivIngestionPipeline-1";
        private const string PrivateCameraIngestionSinkVideoName = PrivateCameraIngestionPipelineName + "camera-001";

        // parameter names 
        private const string RtspUserNameParameterName = "rtspUserNameParameter";
        private const string RtspPasswordParameterName = "rtspPasswordParameter";
        private const string VideoNameParameterName = "videoNameParameter";
        private const string RtspUrlParameterName = "rtspUrlParameter";

        private static VideoAnalyzerClient videoAnalyzerClient;

        public static async Task Main(string[] args)
        {
            await SetupClientAsync();

            await IngestFromPrivateCameraAsync();
        }

        /// <summary>
        /// Ingest from a private camera.
        /// </summary>
        /// <returns>The completion task.</returns>
        private static async Task IngestFromPrivateCameraAsync()
        {
            try
            {
                await CreateTopologyForPrivateCameraAsync();
                Console.WriteLine($"Created topology '{PrivateCameraIngestionTopologyName}'");

                await CreateLivePipelineForPrivateCameraAsync();
                Console.WriteLine($"Created pipeline '{PrivateCameraIngestionPipelineName}'");

                Console.WriteLine($"Activating pipeline '{PrivateCameraIngestionPipelineName}'");
                await ActivateLivePipelineAsync(PrivateCameraIngestionPipelineName);

                Console.WriteLine($"Pipeline '{PrivateCameraIngestionPipelineName}' is activated, please go to portal to play the video '{PrivateCameraIngestionSinkVideoName}'");

                Console.WriteLine("Press enter to deactivate the pipeline and cleanup the resources");

                Console.Read();
            }
            catch (PipelineResponseException ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ex.Response.Content);
                Console.ForegroundColor = ConsoleColor.Gray;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ex.ToString());
                Console.ForegroundColor = ConsoleColor.Gray;
            }
            finally
            {
                Console.WriteLine("cleaning up resources");
                await CleanUpResourcesAsync();
            }
        }

        /// <summary>
        /// Create a live pipeline.
        /// </summary>
        /// <returns>The completion task.</returns>
        private static async Task CreateLivePipelineForPrivateCameraAsync()
        {
            var pipelineModel = CreateLivePipelineModelForPrivateIngestion();
            await videoAnalyzerClient.LivePipelines.CreateOrUpdateAsync(ResourceGroupName, AccountName, PrivateCameraIngestionPipelineName, pipelineModel);
        }

        /// <summary>
        /// Create a topology.
        /// </summary>
        /// <returns>The completion task.</returns>
        private static async Task CreateTopologyForPrivateCameraAsync()
        {
            var topologyModel = CreatePipelineTopologyModelForPrivateCamera();

            await videoAnalyzerClient.PipelineTopologies.CreateOrUpdateAsync(ResourceGroupName, AccountName, PrivateCameraIngestionTopologyName, topologyModel);
        }

        /// <summary>
        /// Activate the live pipeline.
        /// </summary>
        /// <param name="livePipelineName">live pipeline name.</param>
        /// <returns>The completion task.</returns>
        private static async Task ActivateLivePipelineAsync(string livePipelineName)
        {
            await videoAnalyzerClient.LivePipelines.ActivateAsync(ResourceGroupName, AccountName, livePipelineName);
        }

        /// <summary>
        /// Cleanup the resources.
        /// </summary>
        /// <returns>The completion task.</returns>
        private static async Task CleanUpResourcesAsync()
        {
            var response = await GetLivePipelineAsync(PrivateCameraIngestionPipelineName);
            if (response != null)
            {
                if (response.State == LivePipelineState.Active)
                {
                    await videoAnalyzerClient.LivePipelines.DeactivateAsync(ResourceGroupName, AccountName, PrivateCameraIngestionPipelineName);
                    Console.WriteLine($"deactivated pipeline '{PrivateCameraIngestionPipelineName}'");
                }

                await videoAnalyzerClient.LivePipelines.DeleteAsync(ResourceGroupName, AccountName, PrivateCameraIngestionPipelineName);
                Console.WriteLine($"deleted pipeline '{PrivateCameraIngestionPipelineName}'");
            }

            await videoAnalyzerClient.PipelineTopologies.DeleteAsync(ResourceGroupName, AccountName, PrivateCameraIngestionTopologyName);
            Console.WriteLine($"deleted topology '{PrivateCameraIngestionTopologyName}'");
        }

        /// <summary>
        /// Get the live pipeline.
        /// </summary>
        /// <param name="livePipelineName">livePipeline name.</param>
        /// <returns>A task with the livepipeline.</returns>
        private static async Task<LivePipeline> GetLivePipelineAsync(string livePipelineName)
        {
            try
            {
                return await videoAnalyzerClient.LivePipelines.GetAsync(ResourceGroupName, AccountName, livePipelineName);
            }
            catch (PipelineResponseException ex) when (ex.Response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }
        }

        /// <summary>
        /// Setup the client.
        /// </summary>
        /// <returns>The completion task.</returns>
        private async static Task SetupClientAsync()
        {
            var aadSettings = new ActiveDirectoryServiceSettings
            {
                AuthenticationEndpoint = AuthenticationEndpoint,
                TokenAudience = TokenAudience,
                ValidateAuthority = true,
            };

            var clientCredentials = await ApplicationTokenProvider.LoginSilentAsync(TenantId, ClientId, Secret, aadSettings);

            videoAnalyzerClient = new VideoAnalyzerClient(ArmEndPoint, clientCredentials)
            {
                SubscriptionId = SubscriptionId
            };
        }

        /// <summary>
        /// Create a pipeline topology object.
        /// </summary>
        /// <returns>A task with the pipeline topology.</returns>
        private static PipelineTopology CreatePipelineTopologyModelForPrivateCamera()
        {
            return new PipelineTopology(
                name: PrivateCameraIngestionTopologyName,
                description: "The pipeline topology with tunneling between rtsp source and video sink.",
                kind: Kind.Live,
                sku: new Sku(SkuName.LiveS1),
                parameters: new List<ParameterDeclaration>
                {
                    new ParameterDeclaration
                    {
                        Name = RtspUserNameParameterName,
                        Type = "SecretString",
                        Description = "rtsp user name parameter",
                    },
                    new ParameterDeclaration
                    {
                        Name = RtspPasswordParameterName,
                        Type = "SecretString",
                    },
                    new ParameterDeclaration
                    {
                        Name = RtspUrlParameterName,
                        Type = "String",
                    },
                    new ParameterDeclaration
                    {
                        Name = VideoNameParameterName,
                        Type = "String",
                    },
                },
                sources: new List<SourceNodeBase>
                {
                    new RtspSource
                    {
                        Name = "rtspSource",
                        Transport = "tcp",
                        Endpoint = new UnsecuredEndpoint
                        {
                            Url = "${" + RtspUrlParameterName + "}",
                            Credentials = new UsernamePasswordCredentials
                            {
                                Username =  "${" + RtspUserNameParameterName + "}",
                                Password = "${" + RtspPasswordParameterName + "}",
                            },
                            Tunnel = new SecureIotDeviceRemoteTunnel
                            {
                                DeviceId = PrivateCameraIngestionTunnelingDeviceId,
                                IotHubName = iotHubNameForPrivateCameraIngestion,
                            },
                        },
                    },
                },
                sinks: new List<SinkNodeBase>
                {
                    new VideoSink
                    {
                        Name = "videoSink",
                        VideoName = "${" + VideoNameParameterName + "}",
                        Inputs = new List<NodeInput>
                        {
                            new NodeInput("rtspSource"),
                        },
                        VideoCreationProperties = new VideoCreationProperties
                        {
                            Title = "Parking Lot (Camera 1)",
                            Description = "Parking lot south entrance"
                        },
                    },
                });
        }

        /// <summary>
        /// Create a live pipeline object.
        /// </summary>
        /// <param name="description">description of the livepipeline.</param>
        /// <returns>Livepipeline.</returns>
        private static LivePipeline CreateLivePipelineModelForPrivateIngestion(string description = null)
        {
            return new LivePipeline(
              name: PrivateCameraIngestionPipelineName,
              description: description,
              topologyName: PrivateCameraIngestionTopologyName,
              // Maximum capacity in Kbps which is reserved for the live pipeline.
              // if the rtsp source exceeds the capacity, then the service will disconnect temporarily from the camera
              // and will try again to check if camera bitrate is now below the reserved capacity.
              // Allowed range is 500 to 3000 kbps.
              bitrateKbps: 500,
              parameters: new List<ParameterDefinition>
              {
                    new ParameterDefinition(RtspUserNameParameterName, PrivateCameraIngestionSourceRTSPUserName),
                    new ParameterDefinition(RtspPasswordParameterName, PrivateCameraIngestionSourceRTSPPassword),
                    new ParameterDefinition(RtspUrlParameterName, PrivateCameraIngestionSourceRTSPURL),
                    new ParameterDefinition(VideoNameParameterName, PrivateCameraIngestionSinkVideoName),
              });
        }
    }
}
