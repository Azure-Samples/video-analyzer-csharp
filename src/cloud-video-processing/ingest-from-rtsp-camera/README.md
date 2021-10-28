# Azure Video Analyzer sample to capture and ingest video from RTSP camera accessible over the public internet  

This folder contains C# sample for Azure Video Analyzer's preview feature of ingestion from RTSP capable camera over the public internet. 

### Contents

| File             | Description                                                   |
|-------------------------|---------------------------------------------------------------|
| `PublicCameraPipelineSampleCode.csproj`| Project file                                                 |
| `Program.cs`            | The main program file                                         |

### Pre-requisites

1. An Azure account that includes an active subscription. [Create an account](https://azure.microsoft.com/free/?WT.mc_id=A261C142F) for free if you don't already have one.
    * Get your Azure Active Directory [Tenant Id](https://docs.microsoft.com/azure/active-directory/fundamentals/active-directory-how-to-find-tenant)
    * Register an application with Microsoft identity platform to get app registration [Client Id](https://docs.microsoft.com/azure/active-directory/develop/quickstart-register-app#register-an-application) and [Client secret](https://docs.microsoft.com/azure/active-directory/develop/quickstart-register-app#add-a-client-secret)

1. Create a [Video Analyzer account](https://docs.microsoft.com/azure/azure-video-analyzer/video-analyzer-docs/create-video-analyzer-account?tabs=portal) and attach an IoT Hub to the Video Analyzer account.

1. [Visual Studio Code](https://code.visualstudio.com/) on your development machine with following extensions -
    * [Azure IoT Tools](https://marketplace.visualstudio.com/items?itemName=vsciot-vscode.azure-iot-tools)
    * [C#](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csharp)

1. [.NET Core 3.1 SDK](https://dotnet.microsoft.com/download/dotnet-core/3.1) on your development machine.

1. RTSP [capable camera](https://aka.ms/service-supported-cameras) and the RTSP server on this camera needs to be accessible over the public internet. Alternatively, you can deploy an [RTSP camera simulator](https://aka.ms/deploy-rtsp-camsim).

### Setup

- Open your local clone of this git repository in Visual Studio Code.
- Go to `src\cloud-video-processing\ingest-from-rtsp-camera\Program.cs` and provide values for the following variables:

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
| PublicCameraIngestionSourceRTSPURL | Provide RTSP source url  |
| PublicCameraIngestionSourceRTSPUserName | Provide RTSP source username |
| PublicCameraIngestionSourceRTSPPassword | Provide RTSP source password |

- Optionally, you can provide custom values for topology and pipeline parameters, defined just below the variables mentioned in the table (lines 36 - 44).
- Save the changes.

### Code walkthrough

In this section, we will be describing the steps in Program.cs

The Main() is the starting point with two function calls:

```
public static async Task Main(string[] args)
{
    await SetupClientAsync();
    await IngestFromPublicCameraAsync();
}
```

- SetupClientAsync() method is used for service principal authentication.
- IngestFromPublicCameraAsync() method is used to capture and ingest video from a rtsp camera on public network. This method has the following logic:

    1. Create a topology for public camera ingestion in `CreateTopologyForPublicCameraAsync()` method with the following nodes:
        *  RTSP source node
        *  Video sink node

    1. On successful creation of topology, a live pipeline is created in `CreateLivePipelineForPublicCameraAsync()` method using: 
        * Topology and Pipeline names defined in the variables section during setup.
        *  `bitrateKbps` - Bitrate is set to 1500 kbps by default in line 276. Video encoding bitrate must be between 500 and 3000 Kbps. If true ingestion bitrate is above this threshold, ingestion will be disconnected and reconnected with exponential backoff.

    1. On successful completion of pipeline, the pipeline is activated using ActivateLivePipelineAsync() method. This will start your live pipeline and start recording the video.
 
    1. You can playback the recording in Azure portal -> Video Analyzer account blade -> Videos pane.

### Running the sample

Once you have the setup ready with necessary configuration, now is the time to run the sample program:

- Start a debugging session (hit F5). You will start seeing some messages printed in the TERMINAL window denoting topology and pipeline creation. If the creation is successful, the live pipeline is activated and you can go to portal to playback the recording. 
- Login to [Azure portal](https://portal.azure.com/), go to the Azure Video Analyzer account being used for this project.
- Click on Videos blade and choose the video created. Default video name is **PubIngestionPipeline-1-camera-001** stored in variable `PublicCameraIngestionSinkVideoName` in line 38. The video will be in a `Recording` status. 
- Go back to Visual Studio Code TERMINAL window and press enter to deactivate the pipeline and cleanup the resources including pipeline and topology. The recording is persisted and status changes to `Not recording`.

❗**Note:** When running the debugger with the cloud-video-processing/ingest-from-rtsp-camera project, the default launch.json creates a configuration with the parameter "console": "internalConsole". This does not work since internalConsole does not allow keyboard input. Changing the parameter to "console" : "integratedTerminal" fixes the problem.

### Next steps

- [Export portion of recorded video as an MP4 file](../../src/video-export)
- Try the quickstart to create a live pipeline [using Azure portal](https://aka.ms/cloudpipeline)
- Learn more about [live and batch pipelines](https://docs.microsoft.com/azure/azure-video-analyzer/video-analyzer-docs/pipeline)
- [Quotas and limitations](https://aka.ms/livequota) on live pipelines