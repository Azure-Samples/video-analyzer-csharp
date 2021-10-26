# Azure Video Analyzer sample to export portion of recorded video as an MP4 file

This folder contains C# sample for Azure Video Analyzer's feature of export portion of recorded video as an MP4 file.

### Contents

| File             | Description                                                   |
|-------------------------|---------------------------------------------------------------|
| `ExportBatchPipelineJobSampleCode.csproj`| Project file                                                 |
| `Program.cs`            | The main program file                                         |

### Pre-requisites

1. An Azure account that includes an active subscription. [Create an account](https://azure.microsoft.com/free/?WT.mc_id=A261C142F) for free if you don't already have one.
    * Get your Azure Active Directory [Tenant Id](https://docs.microsoft.com/azure/active-directory/fundamentals/active-directory-how-to-find-tenant)
    * Register an application with Microsoft identity platform to get app registration [Client Id](https://docs.microsoft.com/azure/active-directory/develop/quickstart-register-app#register-an-application) and [Client secret](https://docs.microsoft.com/azure/active-directory/develop/quickstart-register-app#add-a-client-secret)

1. Create a [Video Analyzer account](https://docs.microsoft.com/azure/azure-video-analyzer/video-analyzer-docs/create-video-analyzer-account?tabs=portal).

1. [Visual Studio Code](https://code.visualstudio.com/) on your development machine with following extensions -
    * [Azure IoT Tools](https://marketplace.visualstudio.com/items?itemName=vsciot-vscode.azure-iot-tools)
    * [C#](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csharp)

1. [.NET Core 3.1 SDK](https://dotnet.microsoft.com/download/dotnet-core/3.1) on your development machine.

1. RTSP [capable camera](https://aka.ms/service-supported-cameras) OR a video in Azure Video Analyzer service.

### Setup

- Open your local clone of this git repository in Visual Studio Code.
- Go to `src\video-export\Program.cs` and provide values for the following variables:

| Variable       | Description                                |
|----------------------|--------------------------------------------|
| SubscriptionId | Provide Azure subscription Id    |
| ResourceGroup | Provide resource group name |
| AccountName | Provide Azure Video Analyzer account name |
| TenantId | Provide tenant id |
| ClientId | Provide app registration client id |
| Secret | Provide app registration client secret |
| AuthenticationEndpoint | Provide authentication end point (example: https://login.microsoftonline.com) |
| ArmEndPoint | Provide arm end point (example: https://management.azure.com) |
| TokenAudience | Provide token audience (example: https://management.core.windows.net) |
| PublicCameraIngestionSourceRTSPURL *(optional)* | Provide RTSP source url  |
| PublicCameraIngestionSourceRTSPUserName *(optional)* | Provide RTSP source username |
| PublicCameraIngestionSourceRTSPPassword *(optional)* | Provide RTSP source password |

- *The 3 RTSP parameters in the table are optional if you already have a video source.*
- Optionally, you can provide custom values for batch export pipeline parameters and parameter names, defined just below the variables mentioned in the table (lines 42 - 53).
- Save the changes.

### Code walkthrough

In this section, we will be describing the steps in Program.cs

The Main() is the starting point with two function calls:

```
public static async Task Main(string[] args)
{
    await SetupClientAsync();
    await RunExportBatchAsync();
}
```

- SetupClientAsync() method is used for service principal authentication.
- RunExportBatchAsync() method is used to run the batch pipeline job for exporting video as an MP4 file. This method has the following logic:

    1. Setup video ingestion to create source video using public camera ingestion parameters.
        * SetupIngestionToCreateSourceVideoForExportAsync() method starts video ingestion from the URL specified in `PublicCameraIngestionSourceRTSPURL`.
        * If a source video is already available in Azure Video Analyzer Videos list (the source video should be of type `archive`), then this step can be skipped and name of the source video can be directly passed as a parameter along with the time range in CreatePipelineJobAsync() in line 93. To skip this step, comment the code at lines 76, 77 and 80.

    1. Using the video created in above step as the source, create a batch topology.
        * CreateTopologyForBatchExportAsync() method creates a pipeline topology with the properties:
            *  Batch_S1 sku
            *  Video source node
            *  Encoder processor node - Encoder with System Preset configuration. Learn more details about [encoder processor node](#encoder-processor-node).
            *  Video sink node

    1. On successful creation of topology, a pipeline job is created.
        * CreatePipelineJobAsync() method takes 2 parameters:
            *  `PublicCameraIngestionSinkVideoName` - source video name, either created programmatically in step 1 or video name provided by user. Make sure to provide a video name available in the same Video Analyzer account and in an `archive` state. 
            *  `range` - The time sequence, that is the start and end timestamp of the portion of the archived video to be exported, should be specified in UTC time. The maximum span of the time sequence (end timestamp - start timestamp) must be less than or equal to 24 hours.

    1. On successful completion of pipeline, a video recording for a duration specified in `range` is generated. Default value is 5 seconds. Currently, the supported format for exported video is MP4.
    1. You can access this recording in the storage account associated with the Azure Video Analyzer account under Blob containers as a content.mp4 blob. 
    1. You can also playback the recording in Azure portal -> Video Analyzer account blade -> Videos pane.

### Running the sample

Once you have the setup ready with necessary configuration, now is the time to run the sample program:

- Start a debugging session (hit F5). You will start seeing some messages printed in the TERMINAL window denoting topology and pipeline job creation. If the job is successful, you can go to portal to playback the recording. 
- Login to [Azure portal](https://portal.azure.com/), go to the Azure Video Analyzer account being used for this project.
- Click on Videos blade and choose the video recording created. Default video recording name is **PipelineJob-1-camera-001** stored in variable `PipelineExportedVideoName` in line 44.
- Go back to Visual Studio Code TERMINAL window and press enter to cleanup the resources including pipeline job and batch topology. The exported recording is persisted.

❗**Note:** When running the debugger with the video-export project, the default launch.json creates a configuration with the parameter "console": "internalConsole". This does not work since internalConsole does not allow keyboard input. Changing the parameter to "console" : "integratedTerminal" fixes the problem.

### Encoder processor node

The encoder processor node can only be used in batch pipeline topologies and allows you to specify encoding properties when converting the recorded video into the desired format for downstream processing. It only supports encoding video with H.264 codec and audio with AAC codec.

Two types of preset configurations allowed in an encoder processor are: 
* System Preset
* Custom Preset

    The allowed configurations for each preset are listed in the table below. 
     
     | Configuration       | System Preset        | Custom Preset |
     | ----------------| --------------|--------------|
     | Video encoder bitrate kbps      | same as source      | 200 to 16,000 kbps |
     | Frame rate       | same as source      | 0 to 300 |
     | Height    | same as source        | 1 to 4320 |
     | Width    | same as source       | 1 to 8192 |
     | Mode   | Pad        | Pad, PreserveAspectRatio, Stretch |     
     | Audio encoder bitrate kbps  | same as source        | Allowed values: 96, 112, 128, 160, 192, 224, 256 | 

The code uses `EncoderSystemPreset` in CreatePipelineTopologyForExportModel() with `SingleLayer_1080p_H264_AAC` preset type. 

```csharp
Preset = new EncoderSystemPreset
{
    Name = "SingleLayer_1080p_H264_AAC",
},
```

Allowed system preset type names are:

| System Preset Type       | Audio AAC properties        | Video H.264 properties | Scale Mode |
| ----------------| --------------|--------------|--------------|
| SingleLayer_540p_H264_AAC       | same as source      | Bitrate: 2000 kbps, Height: 540, Width: 960 | PreserveAspectRatio |
| SingleLayer_720p_H264_AAC       | same as source      | Bitrate: 3500 kbps, Height: 720, Width: 1280  | PreserveAspectRatio |
| SingleLayer_1080p_H264_AAC      | same as source      | Bitrate: 6000 kbps, Height: 1080, Width: 1920  | PreserveAspectRatio |
| SingleLayer_2160p_H264_AAC      | same as source      | Bitrate: 16000 kbps, Height: 2160, Width: 3840  | PreserveAspectRatio |

If you want to change the video and audio encoding for exported video, use `EncoderCustomPreset`. Sample code to use the `EncoderCustomPreset` property -

```csharp
Preset = new EncoderCustomPreset
{ 
    VideoEncoder = new VideoEncoderH264
    {
        BitrateKbps = "3500",
        FrameRate = "30",
        Scale = new VideoScale
        {
            Height = "1080",
            Width = "1920",
            Mode = "Pad",
        },
    },
    AudioEncoder = new AudioEncoderAac
    {
        BitrateKbps = "96",
    },
},
```

### Next steps

- Try the tutorial to export a portion of recorded video as an MP4 file [using Azure portal](https://aka.ms/export-to-mp4).
- Learn more about [live and batch pipelines](https://docs.microsoft.com/azure/azure-video-analyzer/video-analyzer-docs/pipeline).
- [Quotas and limitations](https://aka.ms/livequota) on batch pipelines.