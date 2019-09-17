#Global Variables
$resourceGroupName = "socexp"
$deploymentName = "socexpdeploy"
$location = "West US 2"
$accountName = "socwslp"
$databaseName = "accessdatadb"
$containerName = "accessdatacollection"
$templateFile = "I:\Ramya\websrvlogprocessor-prod\deployment\1\template.json"
$paramFile = "I:\Ramya\websrvlogprocessor-prod\deployment\1\parameters.json"
$databaseResourceName = $accountName + "/sql/" + $databaseName
$containerResourceName = $accountName + "/sql/" + $databaseName + "/" + $containerName

# Create a Resource Group
#New-AzResourceGroup -Name $resourceGroupName -Location $location

# Create a Deployment
#New-AzResourceGroupDeployment -Name $deploymentName -ResourceGroupName $resourceGroupName -TemplateFile $templateFile -TemplateParameterFile $paramFile                                         

# Create an Azure Cosmos database
$resourceName = $accountName + "/sql/" + $databaseName
$DataBaseProperties = @{
    "resource"=@{"id"=$databaseName}
}
New-AzResource -ResourceType "Microsoft.DocumentDb/databaseAccounts/apis/databases" `
    -ApiVersion "2015-04-08" -ResourceGroupName $resourceGroupName `
    -Name $resourceName -PropertyObject $DataBaseProperties
	
# Create the 
$containerProperties = @{
    "resource"=@{
        "id"=$containerName; 
        "partitionKey"=@{
            "paths"=@("/PartitionKey"); 
            "kind"="Hash"
        }; 
        "indexingPolicy"=@{
            "indexingMode"="Consistent"; 
            "includedPaths"= @(@{
                "path"="/*";
                "indexes"= @(@{
                        "kind"="Range";
                        "dataType"="number";
                        "precision"=-1
                    },
                    @{
                        "kind"="Range";
                        "dataType"="string";
                        "precision"=-1
                    }
                )
            });
            "excludedPaths"= @(@{
                "path"="/myPathToNotIndex/*"
            })
        };
        "uniqueKeyPolicy"= @{
            "uniqueKeys"= @(@{
                "paths"= @(
                    "/PartitionKey"
                )
            })
        };
        "defaultTtl"= 100;
        "conflictResolutionPolicy"=@{
            "mode"="lastWriterWins"; 
            "conflictResolutionPath"="/myResolutionPath"
        }
    };
    "options"=@{ "Throughput"= 400 }
} 

New-AzResource -ResourceType "Microsoft.DocumentDb/databaseAccounts/apis/databases/containers" `
    -ApiVersion "2015-04-08" -ResourceGroupName $resourceGroupName `
    -Name $containerResourceName -PropertyObject $containerProperties