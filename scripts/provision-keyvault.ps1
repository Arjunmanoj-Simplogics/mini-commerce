<#
.SYNOPSIS
  Creates an Azure Key Vault and sample secret placeholders for Mini Commerce CSI.

.PARAMETER ResourceGroup
  Resource group name.

.PARAMETER KeyVaultName
  Globally unique Key Vault name.

.PARAMETER Location
  Azure region.
#>
param(
    [Parameter(Mandatory = $true)]
    [string]$ResourceGroup,

    [Parameter(Mandatory = $true)]
    [string]$KeyVaultName,

    [string]$Location = "eastus"
)

$ErrorActionPreference = "Stop"

Write-Host "Ensuring resource group..."
az group create --name $ResourceGroup --location $Location | Out-Null

Write-Host "Creating Key Vault '$KeyVaultName'..."
az keyvault create `
    --name $KeyVaultName `
    --resource-group $ResourceGroup `
    --location $Location `
    --enable-rbac-authorization false | Out-Null

$secretNames = @(
    "ConnectionStrings--OrderDB",
    "ConnectionStrings--InventoryDB",
    "ConnectionStrings--NotificationDB",
    "ConnectionStrings--AuthDB",
    "ConnectionStrings--CatalogDB",
    "ConnectionStrings--CartDB",
    "ServiceBus--ConnectionString",
    "Jwt--SigningKey"
)

Write-Host "Creating placeholder secrets (replace values before production use)..."
foreach ($name in $secretNames) {
    az keyvault secret set --vault-name $KeyVaultName --name $name --value "REPLACE_ME" | Out-Null
    Write-Host "  - $name"
}

Write-Host ""
Write-Host "Next steps:"
Write-Host "  1. Set real secret values in Key Vault."
Write-Host "  2. Grant your AKS workload identity 'Get' on secrets."
Write-Host "  3. Update deploy/kubernetes/secret-provider-class.yaml with vault name, tenantId, clientID."
Write-Host "  4. Apply service-account.yaml + secret-provider-class.yaml and redeploy pods with CSI volume."
Write-Host ""
Write-Host "Vault URI: https://$KeyVaultName.vault.azure.net/"
