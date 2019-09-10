For Azure Function deployment, refer to the following resource:
https://docs.microsoft.com/en-us/azure/azure-functions/functions-bindings-cosmosdb-v2#output

Specifically:
Install-Package Microsoft.Azure.WebJobs.Extensions.ServiceBus -Version 3.1.0-beta4

When Publishing, add the settings via Publish -> Manage Application Settings