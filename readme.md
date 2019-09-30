# Web Server Log Processor

## Overview

Linux Webservers like nginx and Apache creates a log entry for every resource that is accessed from the WebServer. The log file is typically named as "access.log". Access Log has rich access information that can produce great insight for the content owner. He can derive useful informations like module access count, popular modules, etc.

This project takes access log as input. It parses the access log and produce module statistics. The statistics information is persisted in a Cosmos Database. A power BI module project this information into a nice graphical view.

## Project Structure

### ParserLib

Parser Lib folder is a .NET Core class libarary. This project contains the core logic to parser the Access Log.

### ParserApp

Parser App folder is a .NET Core console Application. This project can be used to test the logic of Parser Library. If there is any bug in the Parsing Logic, this module provides an easy way to test this isolation.

### ParserTest

Parser Test Folder is a .NET Core Unit Test Class Library. There are 4 tests right now.

### ParserFn

Parser Fn Folder is a .NET Core Class Library. This project implements the Azure function code.

#### LogToJson

This is an Azure function that gets triggered when a new access log is posted to an azure blob. The Azure function uses ParserLib project to parse the access log and creates a JSON file as ouput. If the access log contains entry for more than one day, there is an unique JSON entry for every day.

#### JsonToDB

This is an Azure Function that gets triggered when a new json file is posted to an azure blob. The Azure function parses the JSON file and projects the same information in a COSMOS DB Table. The entries in JSON is organized by date. The entries in COSMOS DB is organized by module.

### Deployment

To validate the log parser, we the following resources to be created in an Azure Account. It is always recommended to create a new resource group for the following resources:

1. Storage Account
2. Cosmos DB Account
3. App Service Plan
4. Azure Function

Every user of this project, has to create these resources. Manually creating these entries is very tedious and error-prone. The deployment folder is an automation for resource deployment. There are scripts and template files that enables the automation

#### install.ps1

<br>
<br>
Usage: install.ps1 
<br>
<br>
This powershell scripts installs and imports the AZ module, which has all the useful commandlets for resource deployment. 
<br>

#### deploy.ps1

Usage: deploy.ps1 <subscriptionid> <resourcegroupname> <location> <overwriteresources>
<br>
<br>
Example: deploy.ps1 88888888-3333-2222-1111-000000000000 wslp1 "West US 2" \$true
<br>
<br>
This powershell script, prompts the user to login with his Azure Account. After login, the script will validate the input subscription ID. If this is a valid subscription ID, set the same as active subscription. Start to create the resource group, if it does not exist. Invokes the deployment process. The deployment process creates the database account, storage account, app service plan and then the azure function. After the resources are created, the azure function code deployment is configured, which pulls the source from github, builds and deploys the code. Once the deployment is complete, the script creates the database and collection within the database. This script automatically uses the template.json. The json file should be in the same directory as the script.

##### template.json

This is the template file that encapsulates all the resources that has to be created for the end to end solution. This template creates an app service plan that would cost roughly 50 USD. There is also a azure function setting to allow always on. This results in instantaneous triggers for the azure function.

#### deploy_consumption.ps1

Usage: deploy_consumption.ps1 <subscriptionid> <resourcegroupname> <location> <overwriteresources>
<br>
<br>
Example: deploy_consumption.ps1 88888888-3333-2222-1111-000000000000 wslp1 "West US 2" \$true
<br>
<br>
This powershell script, prompts the user to login with his Azure Account. After login, the script will validate the input subscription ID. If this is a valid subscription ID, set the same as active subscription. Start to create the resource group, if it does not exist. Invokes the deployment process. The deployment process creates the database account, storage account, app service plan and then the azure function. After the resources are created, the azure function code deployment is configured, which pulls the source from github, builds and deploys the code. Once the deployment is complete, the script creates the database and collection within the database. This script automatically uses the template_consumption.json. The json file should be in the same directory as the script.

##### template_consumption.json

This is the template file that encapsulates all the resources that has to be created for the end to end solution. This template creates an app service plan that aligns with consumption plan. The function code, goes to sleep when not in use. The wake up on a trigger is not instananteous and we have seen a delay of 2-5 minute to wake up and start running the azure function.

#### deploy_logicapps.ps1

Usage: deploy.ps1 <subscriptionid> <resourcegroupname> <location> <overwriteresources> <outlookalias>
<br>
<br>
Example: deploy_logicapps.ps1 88888888-3333-2222-1111-000000000000 wslp1 "West US 2" template.json \$true triggerwslp
<br>
<br>
This powershell script, prompts the user to login with his Azure Account. After login, the script will validate the input subscription ID. If this is a valid subscription ID, set the same as active subscription. Start to create the resource group, if it does not exist. Invokes the deployment process. The deployment process creates the database account, storage account, app service plan and then the azure function. After the resources are created, the azure function code deployment is configured, which pulls the source from github, builds and deploys the code. Once the deployment is complete, the script creates the database and collection within the database. This script automatically uses the template_logicapps.json. The json file should be in the same directory as the script.
<br>
There is one manual step after the resource deployment.
Go to Portal -> Navigate to Resource Group -> Click on Outlook API Connection -> Edit API Connection -> Click Authorize
This will prompt to signin with the email that's configured to monitor for access log emails

##### template_logicapps.json

This is the template file that encapsulates all the resources that has to be created for the end to end solution. This template creates an app service plan that would cost roughly 50 USD. There is also a azure function setting to allow always on. This results in instantaneous triggers for the azure function. This template creates resouces for azure logic apps (2 connection objects - outlook and azure blob). The logic app resource wait for an email(with attachment) in the inbox. Once a mail arrives, processes the attachment and creates a blob with the attachment content.

## References:

### Azure Function Code Deployment:

https://docs.microsoft.com/en-us/azure/azure-functions/functions-infrastructure-as-code
<br>
https://raw.githubusercontent.com/Azure/azure-quickstart-templates/master/101-function-app-create-dynamic/azuredeploy.json
<br>
http://www.frankysnotes.com/2019/07/four-ways-to-deploy-your-azure-function.html
<br>
https://edi.wang/post/2019/8/10/create-azure-function-app-with-net-core-and-cd-from-github

### Logic Apps:

https://www.bruttin.com/2017/06/13/deploy-logic-app-with-arm.html
<br>
https://docs.microsoft.com/en-us/azure/logic-apps/logic-apps-azure-resource-manager-templates-overview
<br>
https://docs.microsoft.com/en-us/azure/logic-apps/
<br>
https://docs.microsoft.com/en-us/azure/connectors/connectors-create-api-outlook
<br>
https://vincentlauzon.com/2018/09/25/service-principal-for-logic-app-connector/
