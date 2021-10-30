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

        // private camera parameters for pipeline setup
        private const string PrivateCameraTunnelingDeviceId = "<Provide device Id>";
        private const string IotHubNameForPrivateCamera = "<Provide IoT hub name>";
        private const string PrivateCameraSourceRTSPURL = "<Provide RTSP source url>";
        private const string PrivateCameraSourceRTSPUserName = "<Provide RTSP source username>";
        private const string PrivateCameraSourceRTSPPassword = "<Provide RTSP source password>";
        private const string PrivateCameraVideoName = "<Provide unique video name to capture live video from this RTSP source>";

        private const string PrivateCameraTopologyName = "PrivateTopology-1";
        private const string PrivateCameraPipelineName = "PrivatePipeline-1";    

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
                Console.WriteLine($"Created topology '{PrivateCameraTopologyName}'");

                await CreateLivePipelineForPrivateCameraAsync();
                Console.WriteLine($"Created pipeline '{PrivateCameraPipelineName}'");

                Console.WriteLine($"Activating pipeline '{PrivateCameraPipelineName}'");
                await ActivateLivePipelineAsync(PrivateCameraPipelineName);

                Console.WriteLine($"Pipeline '{PrivateCameraPipelineName}' is activated, please go to portal to play the video '{PrivateCameraVideoName}'");

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
            var pipelineModel = CreateLivePipelineModelForPrivateCamera();
            await videoAnalyzerClient.LivePipelines.CreateOrUpdateAsync(ResourceGroupName, AccountName, PrivateCameraPipelineName, pipelineModel);
        }

        /// <summary>
        /// Create a topology.
        /// </summary>
        /// <returns>The completion task.</returns>
        private static async Task CreateTopologyForPrivateCameraAsync()
        {
            var topologyModel = CreatePipelineTopologyModelForPrivateCamera();

            await videoAnalyzerClient.PipelineTopologies.CreateOrUpdateAsync(ResourceGroupName, AccountName, PrivateCameraTopologyName, topologyModel);
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
            var response = await GetLivePipelineAsync(PrivateCameraPipelineName);
            if (response != null)
            {
                if (response.State == LivePipelineState.Active)
                {
                    await videoAnalyzerClient.LivePipelines.DeactivateAsync(ResourceGroupName, AccountName, PrivateCameraPipelineName);
                    Console.WriteLine($"deactivated pipeline '{PrivateCameraPipelineName}'");
                }

                await videoAnalyzerClient.LivePipelines.DeleteAsync(ResourceGroupName, AccountName, PrivateCameraPipelineName);
                Console.WriteLine($"deleted pipeline '{PrivateCameraPipelineName}'");
            }

            await videoAnalyzerClient.PipelineTopologies.DeleteAsync(ResourceGroupName, AccountName, PrivateCameraTopologyName);
            Console.WriteLine($"deleted topology '{PrivateCameraTopologyName}'");
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
                name: PrivateCameraTopologyName,
                description: "Sample pipeline topology for capture, record, and stream live video from a camera that is behind a firewall",
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
                                DeviceId = PrivateCameraTunnelingDeviceId,
                                IotHubName = IotHubNameForPrivateCamera,
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
                            Title = "Capture and record live video from an RTSP-capable camera behind a firewall", 
                            Description = "Sample to capture and record live video from an RTSP-capable camera that is behind a firewall"
                        },
                    },
                });
        }

        /// <summary>
        /// Create a live pipeline object.
        /// </summary>
        /// <param name="description">description of the livepipeline.</param>
        /// <returns>Livepipeline.</returns>
        private static LivePipeline CreateLivePipelineModelForPrivateCamera(string description = null)
        {
            return new LivePipeline(
              name: PrivateCameraPipelineName,
              description: description,
              topologyName: PrivateCameraTopologyName,
              // Maximum capacity in Kbps which is reserved for the live pipeline.
              // if the rtsp source exceeds the capacity, then the service will disconnect temporarily from the camera
              // and will try again to check if camera bitrate is now below the reserved capacity.
              // Allowed range is 500 to 3000 kbps.
              bitrateKbps: 1500,
              parameters: new List<ParameterDefinition>
              {
                    new ParameterDefinition(RtspUserNameParameterName, PrivateCameraSourceRTSPUserName),
                    new ParameterDefinition(RtspPasswordParameterName, PrivateCameraSourceRTSPPassword),
                    new ParameterDefinition(RtspUrlParameterName, PrivateCameraSourceRTSPURL),
                    new ParameterDefinition(VideoNameParameterName, PrivateCameraSinkVideoName),
              });
        }
    }
}
