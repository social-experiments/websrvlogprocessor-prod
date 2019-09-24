param (
    [string]$subscriptionId 
)

if ($subscriptionId -eq "") {
	Write-Host "Usage: loging.ps1 subscriptionId";
	Exit;
}

#Install the AZ Module If not installed
Write-Host "Installing Azure Module";
Install-Module -Name Az -AllowClobber -Scope AllUsers

#Import the AZ Module
Write-Host "Importing Azure Module";
Import-Module Az

#Login Prompt
Write-Host "Sign-In with Azure Credentials";
Connect-AzAccount | Out-Null

#Set the active Subscription
Write-Host "Attempting to Set Active Subscription to $subscriptionId";
$context = Get-AzSubscription -SubscriptionId $subscriptionId
if ($context -eq $null) {
	Write-Host "Incorrect SubscriptionId. Please check the Subscription Id";
	Exit;
}

Set-AzContext $context  
Write-Host "Successfully Set Active Subscription to $subscriptionId";