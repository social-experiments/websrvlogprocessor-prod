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

############################################################
# Check if the Resource Group Exists
############################################################
$existingRG = Get-AzResourceGroup -Name $resourceGroupName -ErrorAction SilentlyContinue
if (($existingRG -ne $null) -and ($overwriteResources -ne $true)) {
	Write-Host "Resource Group Already Exist.";
	Write-Host "Provide a New Resource Group Name or set overwriteResources parameter to true.";
	Exit;
}

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
	"resourceGroupName" = $trimmedResourceGroupName
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

