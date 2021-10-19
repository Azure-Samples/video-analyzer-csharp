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

namespace PublicCameraPipelineSampleCode
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

        // public camera ingestion parameters for pipeline setup
        private const string PublicCameraIngestionSourceRTSPURL = "<Provide RTSP source url>";
        private const string PublicCameraIngestionSourceRTSPUserName = "<Provide RTSP source username>";
        private const string PublicCameraIngestionSourceRTSPPassword = "<Provide RTSP source password>";

        private const string PublicCameraIngestionTopologyName = "PubIngestionTopology-1";
        private const string PublicCameraIngestionPipelineName = "PubIngestionPipeline-1";
        private const string PublicCameraIngestionSinkVideoName = PublicCameraIngestionPipelineName + "camera-001";

        // parameter names
        private const string RtspUserNameParameterName = "rtspUserNameParameter";
        private const string RtspPasswordParameterName = "rtspPasswordParameter";
        private const string VideoNameParameterName = "videoNameParameter";
        private const string RtspUrlParameterName = "rtspUrlParameter";

        private static VideoAnalyzerClient videoAnalyzerClient;

        public static async Task Main(string[] args)
        {
            await SetupClientAsync();

            await IngestFromPublicCameraAsync();
        }

        /// <summary>
        /// Ingest from a public camera.
        /// </summary>
        /// <returns>The completion task.</returns>
        private static async Task IngestFromPublicCameraAsync()
        {
            try
            {
                await CreateTopologyForPublicCameraAsync();
                Console.WriteLine($"Created topology '{PublicCameraIngestionTopologyName}'");

                await CreateLivePipelineForPublicCameraAsync();
                Console.WriteLine($"Created pipeline '{PublicCameraIngestionPipelineName}'");

                Console.WriteLine($"Activating pipeline '{PublicCameraIngestionPipelineName}'");
                await ActivateLivePipelineAsync(PublicCameraIngestionPipelineName);
                
                Console.WriteLine($"Pipeline '{PublicCameraIngestionPipelineName}' is activated, please go to portal to play the video '{PublicCameraIngestionSinkVideoName}'");

                Console.WriteLine("Press enter to deactivate the pipeline and cleanup the resources");

                Console.Read();
            }
            catch(Exception ex)
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
        /// Create a topology.
        /// </summary>
        /// <returns>The completion task.</returns>
        private static async Task CreateTopologyForPublicCameraAsync()
        {
            var topologyModel = CreatePipelineTopologyModelForPublicCamera();

            await videoAnalyzerClient.PipelineTopologies.CreateOrUpdateAsync(ResourceGroupName, AccountName, PublicCameraIngestionTopologyName, topologyModel);
        }

        /// <summary>
        /// Create a live pipeline.
        /// </summary>
        /// <returns>The completion task.</returns>
        private static async Task CreateLivePipelineForPublicCameraAsync()
        {
            var pipelineModel = CreateLivePipelineModelForPublicIngestion();
            await videoAnalyzerClient.LivePipelines.CreateOrUpdateAsync(ResourceGroupName, AccountName, PublicCameraIngestionPipelineName, pipelineModel);
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
        /// Checks whether pipeline status is Active, if true deactivates it and then do cleanup of resources.
        /// </summary>
        /// <returns>The completion task.</returns>
        private static async Task CleanUpResourcesAsync()
        {
            var response = await GetLivePipelineAsync(PublicCameraIngestionPipelineName);
            if (response != null)
            {
                if (response.State == LivePipelineState.Active)
                {
                    await videoAnalyzerClient.LivePipelines.DeactivateAsync(ResourceGroupName, AccountName, PublicCameraIngestionPipelineName);
                    Console.WriteLine($"deactivated pipeline '{PublicCameraIngestionPipelineName}'");
                }

                await videoAnalyzerClient.LivePipelines.DeleteAsync(ResourceGroupName, AccountName, PublicCameraIngestionPipelineName);
                Console.WriteLine($"deleted pipeline '{PublicCameraIngestionPipelineName}'");
            }

            await videoAnalyzerClient.PipelineTopologies.DeleteAsync(ResourceGroupName, AccountName, PublicCameraIngestionTopologyName);
            Console.WriteLine($"deleted topology '{PublicCameraIngestionTopologyName}'");
        }

        /// <summary>
        /// Get the live pipeline.
        /// </summary>
        /// <param name="livePipelineName">live pipeline name.</param>
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
        private static PipelineTopology CreatePipelineTopologyModelForPublicCamera()
        {
            return new PipelineTopology(
                name: PublicCameraIngestionTopologyName,
                description: "The pipeline topology with rtsp source and video sink.",
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
                                Username = "${" + RtspUserNameParameterName + "}",
                                Password = "${" + RtspPasswordParameterName + "}",
                            },
                        },
                    },
                },
                sinks: new List<SinkNodeBase>
                {
                    new VideoSink
                    {
                        Name = "videoSink",
                        VideoName =  "${" + VideoNameParameterName + "}",
                        Inputs = new List<NodeInput>
                        {
                            new NodeInput("rtspSource"),
                        },
                        VideoCreationProperties = new VideoCreationProperties
                        {
                            Title = "Parking Lot (Camera 1)",
                            Description = "Parking lot south entrance",
                        },
                    },
                });
        }

        /// <summary>
        ///  Create a live pipeline object.
        /// </summary>
        /// <param name="description">description of the livepipeline.</param>
        /// <returns>Livepipeline.</returns>
        private static LivePipeline CreateLivePipelineModelForPublicIngestion(string description = null)
        {
            return new LivePipeline(
              name: PublicCameraIngestionPipelineName,
              description: description,
              topologyName: PublicCameraIngestionTopologyName,
              // Maximum capacity in Kbps which is reserved for the live pipeline.
              // if the rtsp source exceeds the capacity, then the service will disconnect temporarily from the camera
              // and will try again to check if camera bitrate is now below the reserved capacity.
              // Allowed range is 500 to 3000 kbps.
              bitrateKbps: 1500,
              parameters: new List<ParameterDefinition>
              {
                    new ParameterDefinition(RtspUserNameParameterName, PublicCameraIngestionSourceRTSPUserName),
                    new ParameterDefinition(RtspPasswordParameterName, PublicCameraIngestionSourceRTSPPassword),
                    new ParameterDefinition(RtspUrlParameterName, PublicCameraIngestionSourceRTSPURL),
                    new ParameterDefinition(VideoNameParameterName, PublicCameraIngestionSinkVideoName),
              });
        }
    }
}
