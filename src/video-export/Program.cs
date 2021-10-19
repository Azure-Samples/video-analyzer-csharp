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
using Newtonsoft.Json;
using PipelineResponseException = Microsoft.Azure.Management.VideoAnalyzer.Models.ErrorResponseException;

namespace ExportBatchPipelineJobSampleCode
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

        private const string PublicCameraIngestionTopologyName = "PublicIngestionTopology-1";
        private const string PublicCameraIngestionPipelineName = "PublicIngestionPipeline-1";
        private const string PublicCameraIngestionSinkVideoName = PublicCameraIngestionPipelineName + "camera-001";

        // batch export pipeline parameters
        private const string ExportBatchTopologyName = "ExportBatchTopology-1";
        private const string PipelineJobName = "PipelineJob-1";
        private const string PipelineExportedVideoName = PipelineJobName + "camera-001";

        // parameter names
        private const string RtspUserNameParameterName = "rtspUserNameParameter";
        private const string RtspPasswordParameterName = "rtspPasswordParameter";
        private const string VideoNameParameterName = "videoNameParameter";
        private const string RtspUrlParameterName = "rtspUrlParameter";
        private const string VideoSinkNameParameterName = "videoSinkNameParameter";
        private const string VideoSourceTimeSequenceParameterName = "videoSourceTimeSequenceParameter";
        private const string VideoSourceVideoNameParameterName = "videoSourceVideoNameParameter";

        private static VideoAnalyzerClient videoAnalyzerClient;

        public static async Task Main(string[] args)
        {
            await SetupClientAsync();

            await RunExportBatchAsync();
        }

        /// <summary>
        /// Run the batch pipeline job for video export.
        /// </summary>
        /// <returns>The completion task.</returns>
        private static async Task RunExportBatchAsync()
        {
            var clipDurationInSec = 5;
            try
            {
                // start ingesting to create video for export
                // if a source video is already available then this can be skippped
                // and its name can directly be passed as a parameter along with the time range in CreatePipelineJobAsync.
                Console.WriteLine($"Setting up live pipeline ingestion to create source video for export");
                await SetupIngestionToCreateSourceVideoForExportAsync();

                // wait for 10 secs before kicking off the batch pipeline job to have a clip exported for 5 secs.
                await Task.Delay(TimeSpan.FromSeconds(clipDurationInSec * 2));

                await CreateTopologyForBatchExportAsync();
                Console.WriteLine($"Created topology '{ExportBatchTopologyName}'");

                // provide the time range within which the video clip is to be exported.
                string range = JsonConvert.SerializeObject(
                    new DateTime[,]
                    {
                        // 5 secs time range:  StartTime: (Current Time - 7 secs), EndTime: (Current Time - 2 secs)
                        {  DateTime.UtcNow - TimeSpan.FromSeconds(clipDurationInSec + 2), DateTime.UtcNow - TimeSpan.FromSeconds(2) }
                    });

                await CreatePipelineJobAsync(PublicCameraIngestionSinkVideoName, range);
                Console.WriteLine($"Created pipeline job '{PipelineJobName}'");

                if (await WaitForPipelineJobToBeCompletedAsync())
                {
                    Console.WriteLine($"Pipeline job '{PipelineJobName}' is completed, please go to portal to play the video '{PipelineExportedVideoName}'");
                    Console.WriteLine("Press enter to cleanup the resources");
                    Console.Read();
                }
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
                await CleanUpResourcesForExportBatchAsync();
            }
        }

        /// <summary>
        /// Setup the ingestion to create a source video for export.
        /// </summary>
        /// <returns>The completion task.</returns>
        private static async Task SetupIngestionToCreateSourceVideoForExportAsync()
        {
            await CreateTopologyForPublicCameraAsync();
            await CreateLivePipelineForPublicCameraAsync();
            await ActivateLivePipelineAsync(PublicCameraIngestionPipelineName);
        }

        /// <summary>
        /// Creates a topology for public camera ingestion.
        /// </summary>
        /// <returns>The completion task.</returns>
        private static async Task CreateTopologyForPublicCameraAsync()
        {
            var topologyModel = CreatePipelineTopologyModelForPublicCamera();

            await videoAnalyzerClient.PipelineTopologies.CreateOrUpdateAsync(ResourceGroupName, AccountName, PublicCameraIngestionTopologyName, topologyModel);
        }

        /// <summary>
        /// Creates a live pipeline for public camera ingestion.
        /// </summary>
        /// <returns>The completion task.</returns>
        private static async Task CreateLivePipelineForPublicCameraAsync()
        {
            var pipelineModel = CreateLivePipelineModelForPublicIngestion();
            await videoAnalyzerClient.LivePipelines.CreateOrUpdateAsync(ResourceGroupName, AccountName, PublicCameraIngestionPipelineName, pipelineModel);
        }

        /// <summary>
        /// Create a batch pipeline toplogy to be used for batch export.
        /// </summary>
        /// <returns>The completion task.</returns>
        private static async Task CreateTopologyForBatchExportAsync()
        {
            var topologyModel = CreatePipelineTopologyForExportModel();

            await videoAnalyzerClient.PipelineTopologies.CreateOrUpdateAsync(ResourceGroupName, AccountName, ExportBatchTopologyName, topologyModel);
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
        /// Create a pipeline job for export.
        /// </summary>
        /// <param name="sourceVideoName">source video name to export.</param>
        /// <param name="range">time range within which video clip is to be exported.</param>
        /// <returns>The completion task.</returns>
        private static async Task CreatePipelineJobAsync(string sourceVideoName, string range)
        {
            PipelineJob pipelineJob = CreatePipelineJobModel(PipelineJobName, ExportBatchTopologyName, sourceVideoName, PipelineExportedVideoName, range);
            await videoAnalyzerClient.PipelineJobs.CreateOrUpdateAsync(ResourceGroupName, AccountName, PipelineJobName, pipelineJob);
        }

        /// <summary>
        /// Wait for pipeline job to be completed.
        /// </summary>
        /// <returns>A task with a value indicating if pipeline job completed successfully.</returns>
        private static async Task<bool> WaitForPipelineJobToBeCompletedAsync()
        {
            PipelineJob pipelineJob;
            while (true)
            {
                pipelineJob = await GetPipeLineJobAsync(PipelineJobName);

                if (pipelineJob != null && pipelineJob.State == PipelineJobState.Processing)
                {
                    Console.WriteLine($"Pipeline job '{PipelineJobName}' is still running, sleeping for another 5 secs");
                    await Task.Delay(5000);
                }
                else if (pipelineJob != null && pipelineJob.State == PipelineJobState.Failed)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Pipeline job '{PipelineJobName}' failed with error - {pipelineJob.Error.Message}");
                    Console.ForegroundColor = ConsoleColor.Gray;
                    return false;
                }
                else
                {
                    return pipelineJob != null && pipelineJob.State == PipelineJobState.Completed;
                }
            }
        }

        /// <summary>
        /// Cleanup the resources created for batch export.
        /// </summary>
        /// <returns>The completion task.</returns>
        private static async Task CleanUpResourcesForExportBatchAsync()
        {
            var response = await GetPipeLineJobAsync(PipelineJobName);
            if (response != null)
            {
                if (response.State == PipelineJobState.Processing)
                {
                    await videoAnalyzerClient.PipelineJobs.BeginCancelAsync(ResourceGroupName, AccountName, PipelineJobName);
                }

                await videoAnalyzerClient.PipelineJobs.DeleteAsync(ResourceGroupName, AccountName, PipelineJobName);
                Console.WriteLine($"deleted pipeline job '{PipelineJobName}'");
            }

            await videoAnalyzerClient.PipelineTopologies.DeleteAsync(ResourceGroupName, AccountName, ExportBatchTopologyName);
            Console.WriteLine($"deleted batch topology '{ExportBatchTopologyName}'");

            await CleanupLivePipelineResources(PublicCameraIngestionTopologyName, PublicCameraIngestionPipelineName);
        }

        /// <summary>
        /// Cleanup the live pipeline resources created.
        /// </summary>
        /// <param name="topologyName">topology name.</param>
        /// <param name="pipelineName">pipeline name.</param>
        /// <returns></returns>
        private static async Task CleanupLivePipelineResources(string topologyName, string pipelineName)
        {
            var response = await GetLivePipelineAsync(pipelineName);
            if (response != null)
            {
                if (response.State == LivePipelineState.Active)
                {
                    await videoAnalyzerClient.LivePipelines.DeactivateAsync(ResourceGroupName, AccountName, pipelineName);
                    Console.WriteLine($"deactivated pipeline '{pipelineName}'");
                }

                await videoAnalyzerClient.LivePipelines.DeleteAsync(ResourceGroupName, AccountName, pipelineName);
                Console.WriteLine($"deleted pipeline '{pipelineName}'");
            }

            await videoAnalyzerClient.PipelineTopologies.DeleteAsync(ResourceGroupName, AccountName, topologyName);
            Console.WriteLine($"deleted topology '{topologyName}'");
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
        /// Get the pipeline job.
        /// </summary>
        /// <param name="pipelineJobName">pipeline job name.</param>
        /// <returns>A task with the pipelinejob.</returns>
        private static async Task<PipelineJob> GetPipeLineJobAsync(string pipelineJobName)
        {
            try
            {
                return await videoAnalyzerClient.PipelineJobs.GetAsync(ResourceGroupName, AccountName, pipelineJobName);
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
        /// Create a pipeline topology object for ingestion from a public camera.
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
        /// Create a export batch pipeline topology object.
        /// </summary>
        /// <param name="description">description on the topology.</param>
        /// <returns>A task with the pipeline topology.</returns>
        private static PipelineTopology CreatePipelineTopologyForExportModel(string description = null)
        {
            return new PipelineTopology(
                name: ExportBatchTopologyName,
                description: description,
                kind: Kind.Batch,
                sku: new Sku(SkuName.BatchS1),
                parameters: new List<ParameterDeclaration>
                {
                    new ParameterDeclaration
                    {
                        Name = VideoSourceVideoNameParameterName,
                        Type = "string",
                        Description = "video source video name parameter",
                    },
                    new ParameterDeclaration
                    {
                        Name = VideoSourceTimeSequenceParameterName,
                        Type = "string",
                    },
                    new ParameterDeclaration
                    {
                        Name = VideoSinkNameParameterName,
                        Type = "string",
                    },
                },
                sources: new List<SourceNodeBase>
                {
                    new VideoSource
                    {
                        Name = "videoSource",
                        VideoName = "${" + VideoSourceVideoNameParameterName + "}",
                        TimeSequences = new VideoSequenceAbsoluteTimeMarkers
                        {
                            Ranges = "${" + VideoSourceTimeSequenceParameterName + "}",
                        },
                    },
                },
                processors: new List<ProcessorNodeBase>
                {
                    new EncoderProcessor
                    {
                        Name = "encoderProcessor",
                        Inputs = new List<NodeInput>
                        {
                            new NodeInput
                            {
                                NodeName = "videoSource",
                            },
                        },
                        Preset = new EncoderCustomPreset
                        {
                            VideoEncoder = new VideoEncoderH264
                            {
                                BitrateKbps = "3500",
                                FrameRate = "30",
                                Scale = new VideoScale
                                {
                                    Height = "3840",
                                    Width = "2160",
                                    Mode = "Pad",
                                },
                            },
                            AudioEncoder = new AudioEncoderAac
                            {
                                BitrateKbps = "96",
                            },
                        },
                    },
                },
                sinks: new List<SinkNodeBase>
                {
                    new VideoSink
                    {
                        Name = "videoSink",
                        VideoName = "${" + VideoSinkNameParameterName + "}",
                        Inputs = new List<NodeInput>
                        {
                            new NodeInput("encoderProcessor"),
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
        /// Create a live pipeline object.
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

        /// <summary>
        /// Create a live pipeline job object.
        /// </summary>
        /// <param name="pipelineJobName">pipeline job name.</param>
        /// <param name="pipelineTopologyName">pipeline topology name.</param>
        /// <param name="sourceVideoName">source video name.</param>
        /// <param name="sinkVideoName">sink video name.</param>
        /// <param name="range">time range within which the video clip is to be exported.</param>
        /// <param name="description">description of the pipeline job.</param>
        /// <returns></returns>
        private static PipelineJob CreatePipelineJobModel(string pipelineJobName, string pipelineTopologyName, string sourceVideoName, string sinkVideoName, string range, string description = null)
        {
            return new PipelineJob(
              name: pipelineJobName,
              description: description,
              topologyName: pipelineTopologyName,
              parameters: new List<ParameterDefinition>
              {
                    new ParameterDefinition(VideoSourceVideoNameParameterName, sourceVideoName),
                    new ParameterDefinition(VideoSourceTimeSequenceParameterName, range),
                    new ParameterDefinition(VideoSinkNameParameterName, sinkVideoName),
              });
        }
    }
}
