# Introduction 
E-Forms Function Apps 

# Getting Started
Clone the repository locally to get started
1. Copy the below and create `local.settings.json` at the root of the eforms_middleware project
```json
{
  "IsEncrypted": false,
  "Values": {
    "FUNCTIONS_WORKER_RUNTIME": "dotnet",
    "VaultUri": "https://dot-az-dev-aue-kvault-fm.vault.azure.net/",
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "environment": "dev",
    "FromEmail": "forms-dev@transport.wa.gov.au",
    "ToEmail": "DOTSharePointDEV.Notification@transport.wa.gov.au",
    "SMTPHost": "mailhost01.dpi.wa.gov.au",
    "IsImpersonationAllowed": true,
    "AzureFunctionsJobHost__logging__applicationInsights__samplingSettings__isEnabled": false,
    "AzureWebJobs.func-create-trelis-notification-timer.Disabled": true,
    "AzureWebJobs.func-create-trelis-monthly-return-timer.Disabled": true,
    "AzureWebJobs.func-create-coi-notification-timer.Disabled": true,
    "AzureWebJobs.cpr-12-month-from-completion-notification.Disabled": true,
    "AzureWebJobs.func-notification-timer.Disabled": true,
    "AzureWebJobs.migrate-blobs-timer.Disabled": true,
    "Blob:ConnectionString": "UseDevelopmentStorage=true",
    "Blob:ContainerName": "eforms-blob",
    "BaseEformsUrl": "http://localhost:4200",
    "WEBSITE_TIME_ZONE": "W. Australia Standard Time",
    "ReleaseTwo": true,
    "CoiOtherAvailable": false
  },
  "ConnectionStrings": {
    "AZURESQL-ConnectionString": "<CONNECTION_STRING>",
    "AZUREAD-Secret": "<SECRET>",
    "TEST": "TEST1"
  },
  "Host": {
    "LocalHttpPort": 7071,
    "CORS": "*"
  }
}
```
2. Update `<CONNECTION_STRING>` and `<SECRET>` with the correct value which the other developers should have.
3. Make sure that you have Azurite installed which you can get by following the steps [here](https://docs.microsoft.com/en-us/azure/storage/common/storage-use-azurite?tabs=visual-studio) for your preferred platform
4. Install postman or some other tool with similar functionality
5. Ensure that you are logged in to Visual Studio with your account as DB Auth uses that
6. Install Azure Storage Explorer from [here](https://azure.microsoft.com/en-us/products/storage/storage-explorer/#overview)
7. Connect to your local storage using Azure Storage Explorer and create `eforms-blob` Blob Container

**NOTE** it should be possible to just login via the shell using `az login` but it appears to be blocked.

### Settings values
Below is a guide to what the above `local.settings.json` values do in case they need to be toggled:

| Value | Description                                                                                           |
|-----------|-------------------------------------------------------------------------------------------------------|
|IsImpersonationAllowed| Enable or disable the user impersonation feature of the functions. **Never enable this in production** |
|ReleaseTwo| Enable or dissable items for release 2. Temporary workaround until feature flags can be added.        |
|CoiOtherAvailable| Enable or disable the CoiOther form type. Temporary workaround until feature flags can be added.      |
|AzureWebJobs.func-create-trelis-notification-timer.Disabled| Is the timed job of the same name disabled. By default locally it should be disabled unless testing   |
|AzureWebJobs.func-create-trelis-monthly-return-timer.Disabled| Is the timed job of the same name disabled. By default locally it should be disabled unless testing   |

# Build and Test
Running the default build is sufficient when developing antd testing locally. Simply make sure that Azurite
is running and start the functions in your IDE of choice. To test function changes follow the steps below:

## Testing HTTP Triggers
1. Open PostMan
2. Choose the Http Verb of the method being called from the dropdown
3. In the URL bar add `http://localhost:7071/api/<FUNCTION_NAME>` replacing `<FUNCTION_NAME>` with the method to be tested
4. Add the following headers

| Header Key      | Description                                                                           |
|-----------------|---------------------------------------------------------------------------------------|
| upn             | This can be any email value but represents the actual logged in user making the call. |
| Requesting-User | This can be any email but represents the user that the angular is acting as.          |
5. Depending on the call you may also have to provide `Params` or a `Body` so check what the method requires
6. Click `Send` to trigger the request and call the local function app

## Testing Non-Http Triggers
1. Open PostMan
2. Choose `Post` as the Verb of the method being called from the dropdown
3. In the URL bar add `http://localhost:7071/admin/functions/<FUNCTION_NAME>` replacing `<FUNCTION_NAME>` with the name of the function being tested
4. Choose the `Body` tab and select `raw` from the select options 
5. Then in the dropdown to the right choose `JSON`
6. In the body area add `{ "input": "test" }`
7. Click the `Send` button to trigger the request

## Disabling functions locally
Some functions need to be disabled locally to prevent unexpected issues such as a Timer Trigger running when it isn't meant to.

There are many ways to disable but so as to not accidentally disable functions in an environment it is recommended to do it via the `local.settings.json`.

To disable a function add `AzureWebJobs.<FUNCTION_NAME>.Disabled: true` into the values object. An example can be seen there already: `"AzureWebJobs.func-create-trelis-monthly-return-timer.Disabled": true`.

## Adding extra logging
Currently there is a known issue with Azure Function logging that means some logging is omitted by App Insights if it comes from the Dependency Injected logger.

To have these additional logs appear use the following host.json value replacing the values in `logLevel` with any namespace where logging should occur.
```json
{
  "version": "2.0",
  "logging": {
    "applicationInsights": {
      "samplingSettings": {
        "isEnabled": true,
        "excludedTypes": "Request;Exception"
      }
    },
    "logLevel": {
      "Default": "Error",
      "Microsoft": "Warning",
      "Microsoft.Hosting.Lifetime": "Information",
      "Microsoft.EntityFrameworkCore.Database.Command": "Error",
      "eforms_middleware": "Information"
    }
  }
}
```

# Entity Framework
## Setup
1. Install the latest version of the nuget.exe from [here](https://www.nuget.org/downloads)
2. Move the exe that was downloaded to a directory of your choice and record the path
3. Add the path recorded above to the environment variables path
4. Restart all PowerShell instances
5. Check that the above has worked by running `nuget.exe` in a new PowerShell terminal
6. Run `nuget.exe config -set http_proxy=http://proxy.dpi.wa.gov.au:3128`
7. Run `nuget.exe config -set http_proxy.user=dpi\<USERNAME>` replacing `<USERNAME>` with your username
8. You should now have a `NuGet.Config` in `C:\Users\<USERNAME>\AppData\Roaming\NuGet`. Confirm it is there.
9. Run `dotnet tool install --global dotnet-ef --configfile C:\Users\<USERNAME>\AppData\Roaming\NuGet\NuGet.Config` replacing `<USERNAME>` with your username
10. Test EF installed correctly by running `dotnet ef`

## Using EF
Install SQL developer edition and deploy the database locally. Then run the [ScaffoldDb.ps1 here](./eforms_middleware/scripts/ScaffoldDb.ps1).

## Adding new Entities/Tables
In order to add new entities to the Scaffolding you will need to update the script. At the end simply add `-t <TABLE_NAME>` where `<TABLE_NAME>` is the name of the DB Table to be scaffolded.

# Contribute
TODO: Explain how other users and developers can contribute to make your code better. 

If you want to learn more about creating good readme files then refer the following [guidelines](https://docs.microsoft.com/en-us/azure/devops/repos/git/create-a-readme?view=azure-devops). You can also seek inspiration from the below readme files:
- [ASP.NET Core](https://github.com/aspnet/Home)
- [Visual Studio Code](https://github.com/Microsoft/vscode)
- [Chakra Core](https://github.com/Microsoft/ChakraCore)
- [Disable functions](https://docs.microsoft.com/en-us/azure/azure-functions/disable-function?tabs=portal#localsettingsjson)
- [EF DB Scaffolding](https://docs.microsoft.com/en-us/ef/core/managing-schemas/scaffolding?tabs=dotnet-core-cli)