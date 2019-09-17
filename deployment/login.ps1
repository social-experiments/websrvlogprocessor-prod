Install-Module -Name Az -AllowClobber -Scope AllUsers
Import-Module Az
Connect-AzAccount
$context = Get-AzSubscription -SubscriptionId "6c41da6c-2ea2-4390-bb4f-a9c5c0573907"
Set-AzContext $context  