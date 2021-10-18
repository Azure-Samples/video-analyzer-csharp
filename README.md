---
page_type: sample
languages:
  - csharp
products:
  - azure
  - azure-video-analyzer
description: "The samples in this repo show how to use the Azure Video Analyzer module to analyze live video using AI modules of your choice and optionally record video on the edge device or in the cloud."  
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

* **cloud-video-processing** - This folder contains a dotnet core console app that enables you to ***To-do***.
* **video-export** - This folder has a ***To-do***.
* **video-consumption** - This folder contains two parts:
 
    * **token-issuer**: an application for generating [JSON Web Tokens](https://datatracker.ietf.org/doc/html/rfc7519). The output can then be used with the [Azure Video Analyzer player widgets](https://docs.microsoft.com/en-us/azure/azure-video-analyzer/video-analyzer-docs/player-widget) for playing back video recordings.
    * **video-player**: This folder contains a ReactJS app that will enable you to view videos and create zones (line or polygon) using the AVA widget player.

## Prerequisites

1. An active Azure subscription
2. Azure resources deployed in the Azure subscription

    a. Video Analyzer account

    b. Storage account

    c. Managed Identity

    d. IoT Hub
    

3. [Visual Studio Code](https://code.visualstudio.com/) on your development machine with following extensions

    a. [Azure IoT Tools](https://marketplace.visualstudio.com/items?itemName=vsciot-vscode.azure-iot-tools)

    b. [C#](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csharp)

4. [.NET Core 3.1 SDK](https://dotnet.microsoft.com/download/dotnet-core/3.1) on your development machine

5. [Register App](https://docs.microsoft.com/azure/active-directory/develop/quickstart-register-app)

## Setup ***To-do***

After cloning the repository, follow instructions outlined in **src/cloud-to-device-console-app/readme.md** to setup the console app.

## Running the sample ***To-do***

Follow instructions outlined in **src/cloud-to-device-console-app/readme.md** to run the console app.

## Key concepts

Read [Azure Video Analyzer concepts](https://docs.microsoft.com/azure/azure-video-analyzer/video-analyzer-docs/overview)

## Resources

- [azure video analyzer sdk](https://github.com/Azure/azure-sdk-for-net/tree/main/sdk/videoanalyzer): contains the source code for Azure Video analyzer C# SDK.
- [Azure Video Analyzer Documentation](https://docs.microsoft.com/azure/azure-video-analyzer/video-analyzer-docs/overview)
- Get started with [Video Analyzer cloud pipeline](https://aka.ms/cloudpipeline)
