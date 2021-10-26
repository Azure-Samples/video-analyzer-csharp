---
page_type: sample
languages:
  - csharp
products:
  - azure
  - azure-video-analyzer
description: "The samples in this repo show how to use the Azure Video Analyzer service to record video using a RTSP capable camera and export portion of recording as an MP4 file."  
---

# Azure Video Analyzer samples

This repository contains C# samples for Azure Video Analyzer

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

* **cloud-video-processing** - This folder contains a dotnet core console app that enables you to ingest videos from rtsp camera.

* **video-consumption** - This folder contains two parts:
 
    * **token-issuer**: an application for generating [JSON Web Tokens](https://datatracker.ietf.org/doc/html/rfc7519). The output can then be used with the Video Analyzer [player widgets](https://docs.microsoft.com/azure/azure-video-analyzer/video-analyzer-docs/player-widget) for playback of video recordings.
    * **video-player**: This folder contains a ReactJS app that will enable you to view videos and create zones (line or polygon) using the Video Analyzer player widget.

* **video-export** - This folder has a source code to export portion of a recorded video as an MP4 file.

## Prerequisites

1. An Azure account that includes an active subscription. [Create an account](https://azure.microsoft.com/free/?WT.mc_id=A261C142F) for free if you don't already have one.
    * Get your Azure Active Directory [Tenant Id](https://docs.microsoft.com/azure/active-directory/fundamentals/active-directory-how-to-find-tenant)
    * Register an application with Microsoft identity platform to get app registration [Client Id](https://docs.microsoft.com/azure/active-directory/develop/quickstart-register-app#register-an-application) and [Client secret](https://docs.microsoft.com/azure/active-directory/develop/quickstart-register-app#add-a-client-secret)

1. Azure resources deployed in the Azure subscription -

    a. [Video Analyzer account](https://docs.microsoft.com/azure/azure-video-analyzer/video-analyzer-docs/create-video-analyzer-account?tabs=portal)

    b. Storage account

    c. Managed Identity

    d. IoT Hub

1. [Visual Studio Code](https://code.visualstudio.com/) on your development machine with following extensions -
    * [Azure IoT Tools](https://marketplace.visualstudio.com/items?itemName=vsciot-vscode.azure-iot-tools)
    * [C#](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csharp)

1. [.NET Core 3.1 SDK](https://dotnet.microsoft.com/download/dotnet-core/3.1) on your development machine.

Set up Azure resources:

[![Deploy to Azure](https://aka.ms/deploytoazurebutton)](https://aka.ms/ava-click-to-deploy)

## Setup

After cloning the repository, follow instructions outlined in each folder to setup the respective app.

## Key concepts

- [azure video analyzer sdk](https://github.com/Azure/azure-sdk-for-net/tree/main/sdk/videoanalyzer): contains the source code for Azure Video analyzer C# SDK.
- [Azure Video Analyzer Documentation](https://docs.microsoft.com/azure/azure-video-analyzer/video-analyzer-docs/overview)
- Get started with [Video Analyzer cloud pipeline](https://aka.ms/cloudpipeline)

## Code of conduct

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/). For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/).
