param (
    [string]$resourceGroupName,
    [string]$location,
	[string]$templateFile,
	[bool]$overwriteResources = $false
)

if (($resourceGroupName -eq "") -or ($location -eq "") -or ($templateFile -eq "")) {
	Write-Host "Usage 1: deploy.ps1 resourceGroupName location templateFile";
	Write-Host "Usage 2: deploy.ps1 resourceGroupName location templateFile overwriteResources";
	Write-Host "Example 1: deploy.ps1 socexp 'West US 2' C:\template.json";
	Write-Host "Example 2: deploy.ps1 socexp 'West US 2' C:\template.json true";
	Exit;
}

if (!(Test-Path $templateFile)) {
	Write-Host "Template File does not exist. Please provide a valid file";
	Exit;
}


############################################################
#Resource Names
############################################################
$trimmedResourceGroupName = $resourceGroupName.ToLower().Replace("_", "")
$deploymentName = $trimmedResourceGroupName + "wslp" + "deploy"

$storageAccountName = $trimmedResourceGroupName + "wslp" + "stor"
$appServicePlanName = $trimmedResourceGroupName + "wslp" + "asp"
$appInsightName = $trimmedResourceGroupName + "wslp" + "ai"
$azureFnName = $trimmedResourceGroupName + "wslp" + "azfn"

$cosmosAccountName = $trimmedResourceGroupName + "wslp" + "db"
$databaseName = "accessdatadb"
$containerName = "accessdatacollection"
$databaseResourceName = $cosmosAccountName + "/sql/" + $databaseName
$containerResourceName = $cosmosAccountName + "/sql/" + $databaseName + "/" + $containerName

############################################################
# Check if the Resource Group Exists
############################################################
$existingRG = Get-AzResourceGroup -Name $resourceGroupName -ErrorAction SilentlyContinue
if (($existingRG -ne $null) -and ($overwriteResources -ne $true)) {
	Write-Host "Resource Group Already Exist.";
	Write-Host "Provide a New Resource Group Name or set overwriteResources parameter to true.";
	Exit;
}
Write-Host "Resource Group Exists.";

############################################################
# Create a Resource Group
############################################################
if (($existingRG -eq $null)) {
	Write-Host "Creating Resource Group.";
	$existingRG = New-AzResourceGroup -Name $resourceGroupName -Location $location
	if (($existingRG -eq $null)) {
		Write-Host "Not able to create Resource Group. Check if sufficient permissions exist for the account. Exiting"
		Exit;
	}
	Write-Host "Resource Group Successfully Created.";
}

$parameterObject = @{
	"serverfarms_wslp_name" = $appServicePlanName
	"sites_wslpparserfn_name" = $azureFnName
	"storageAccounts_wslp_name" = $storageAccountName
	"databaseAccounts_wslp_name" = $cosmosAccountName
	"components_wslpparserfnai_name" = $appInsightName	
}

############################################################
# Create a Deployment
############################################################
Write-Host "Deploying Resources.";
$deployment = New-AzResourceGroupDeployment -Name $deploymentName -ResourceGroupName $resourceGroupName -TemplateFile $templateFile -TemplateParameterObject $parameterObject                                         
if (($deployment -eq $null)) {
	Write-Host "Error Deploying the Resources. Check if sufficient permissions exist for the account. Exiting"
	Exit;
}
Write-Host "Resource successfully Deployed.";

############################################################
# Create an Azure Cosmos database
############################################################
Write-Host "Creating Cosmos Database";
$dataBaseProperties = @{
    "resource"=@{"id"=$databaseName}
}
$database = New-AzResource -ResourceType "Microsoft.DocumentDb/databaseAccounts/apis/databases" `
    -ApiVersion "2015-04-08" -ResourceGroupName $resourceGroupName `
    -Name $databaseResourceName -PropertyObject $dataBaseProperties
if (($database -eq $null)) {
	Write-Host "Error creating the database. Check if sufficient permissions exist for the account. Exiting"
	Exit;
}
Write-Host "Successfully created Database.";

############################################################
# Create the Collection inside Database
############################################################
Write-Host "Creating Collection inside the Cosmos Database";
$containerProperties = @{
    "resource"=@{
        "id"=$containerName; 
        "partitionKey"=@{
            "paths"=@("/PartitionKey"); 
            "kind"="Hash"
        }; 
        "uniqueKeyPolicy"= @{
            "uniqueKeys"= @(@{
                "paths"= @(
                    "/PartitionKey"
                )
            })
        };
    };
    "options"=@{ "Throughput"= 400 }
} 
$collection = New-AzResource -ResourceType "Microsoft.DocumentDb/databaseAccounts/apis/databases/containers" `
    -ApiVersion "2015-04-08" -ResourceGroupName $resourceGroupName `
    -Name $containerResourceName -PropertyObject $containerProperties
if (($collection -eq $null)) {
	Write-Host "Error creating the collection. Check if sufficient permissions exist for the account. Exiting"
	Exit;
}
Write-Host "Successfully created collection.";