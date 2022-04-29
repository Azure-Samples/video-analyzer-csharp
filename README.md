---
page_type: sample
languages:
  - csharp
products:
  - azure
  - azure-video-analyzer
description: "The samples in this repo show how to use the Azure Video Analyzer service to capture, record, and playback live video from an RTSP capable camera and export portion of the video recording as an MP4 file."  
---

# Deprecated - Azure Video Analyzer samples

We’re retiring the Azure Video Analyzer preview service, you're advised to transition your applications off of Video Analyzer by 01 December 2022. This repo is no longer being maintained.

## Contents

| File/folder       | Description                                |
|----------------------|--------------------------------------------|
| `src`                | Sample source code.                        |
| `.gitignore`         | Defines what to ignore at commit time.     |
| `README.md`          | This README file.                          |
| `LICENSE`            | The license for the sample.                |
| `SECURITY`           | Guidelines for reporting security issues   |
| `CODE_OF_CONDUCT.md` | Open source code of conduct                |

The 'src' folder contains the following sub-folders:

* **cloud-video-processing** - This folder contains .NET Core console apps that enable you to capture and record live video from an RTSP-capable camera.

* **video-consumption** - This folder contains two samples:
 
    * **token-issuer**: An application for generating [JSON Web Tokens](https://datatracker.ietf.org/doc/html/rfc7519). The output can then be used with the Video Analyzer [player widgets](https://docs.microsoft.com/azure/azure-video-analyzer/video-analyzer-docs/player-widget) for playback of video.
    * **video-player**: This folder contains a ReactJS app that will enable you to view videos and create zones (lines or polygons) using the Video Analyzer player widget.

* **video-export** - This folder contains a .NET Core console app that enables you to export a portion of a recorded video as an MP4 file.

## Prerequisites

1. An Azure account that includes an active subscription. [Create an account](https://azure.microsoft.com/free/?WT.mc_id=A261C142F) for free if you don't already have one.
    * Get your Azure Active Directory [Tenant Id](https://docs.microsoft.com/azure/active-directory/fundamentals/active-directory-how-to-find-tenant)
    * Register an application with Microsoft identity platform to get app registration [Client Id](https://docs.microsoft.com/azure/active-directory/develop/quickstart-register-app#register-an-application) and [Client secret](https://docs.microsoft.com/azure/active-directory/develop/quickstart-register-app#add-a-client-secret)

1. Azure resources deployed in the Azure subscription -

    a. [Video Analyzer account](https://docs.microsoft.com/azure/azure-video-analyzer/video-analyzer-docs/create-video-analyzer-account?tabs=portal)

    b. Storage account

    c. Managed Identity

    d. Attach a [new](https://docs.microsoft.com/azure/iot-hub/iot-hub-create-through-portal) or [existing IoT Hub to Video Analyzer](https://docs.microsoft.com/azure/azure-video-analyzer/video-analyzer-docs/create-video-analyzer-account?tabs=portal)

1. [Visual Studio Code](https://code.visualstudio.com/) on your development machine with following extensions -
    * [Azure IoT Tools](https://marketplace.visualstudio.com/items?itemName=vsciot-vscode.azure-iot-tools)
    * [C#](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csharp)

1. [.NET Core 3.1 SDK](https://dotnet.microsoft.com/download/dotnet-core/3.1) on your development machine.

## Setup

After cloning the repository, follow instructions outlined in each folder to setup the respective app.

## Next steps

* [Ingest videos from an RTSP camera behind a firewall](./src/cloud-video-processing/capture-from-rtsp-camera-behind-firewall) 
* [Ingest videos from an RTSP camera accessible on the internet](./src/cloud-video-processing/capture-from-rtsp-camera)
* [Export a portion of a recorded video as an MP4 file](./src/video-export)
* Generate a [JSON Web Token](./src/video-consumption/token-issuer) to use with [Video Analyzer widget player](./src/video-consumption/video-player)

## Related links

- [Video Analyzer SDK](https://github.com/Azure/azure-sdk-for-net/tree/main/sdk/videoanalyzer): contains the source code for Video Analyzer C# SDK
- [Video Analyzer Documentation](https://docs.microsoft.com/azure/azure-video-analyzer/video-analyzer-docs/overview)
- Get started with [Video Analyzer service](https://aka.ms/cloudpipeline)

## Code of conduct

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/). For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/).
