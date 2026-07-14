<#
.SYNOPSIS
  Creates an Azure Key Vault and placeholder secrets for Mini Commerce.

.DESCRIPTION
  Secrets use '--' which maps to ':' in ASP.NET Core configuration
  (e.g. Jwt--SigningKey -> Jwt:SigningKey).

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

# JWT, SQL connection strings, Service Bus, Blob Storage, Application Insights, API keys
$secretNames = @(
    "Jwt--SigningKey",
    "Jwt--Issuer",
    "Jwt--Audience",
    "ConnectionStrings--OrderDB",
    "ConnectionStrings--InventoryDB",
    "ConnectionStrings--NotificationDB",
    "ConnectionStrings--AuthDB",
    "ConnectionStrings--CatalogDB",
    "ConnectionStrings--CartDB",
    "ServiceBus--ConnectionString",
    "ServiceBus--FullyQualifiedNamespace",
    "BlobStorage--ConnectionString",
    "BlobStorage--ServiceUri",
    "BlobStorage--AccountName",
    "ApplicationInsights--ConnectionString",
    "ApiKeys--Internal"
)

Write-Host "Creating placeholder secrets (replace values before production use)..."
foreach ($name in $secretNames) {
    az keyvault secret set --vault-name $KeyVaultName --name $name --value "REPLACE_ME" | Out-Null
    Write-Host "  - $name"
}

Write-Host ""
Write-Host "Next steps:"
Write-Host "  1. Set real secret values in Key Vault (never commit them)."
Write-Host "  2. Grant the app Managed Identity 'Key Vault Secrets User' on this vault."
Write-Host "  3. Set KeyVault__Enabled=true and KeyVault__VaultUri=https://$KeyVaultName.vault.azure.net/"
Write-Host "  4. Production uses Managed Identity; local optional use: KeyVault__UseManagedIdentity=false + az login."
Write-Host "  5. Optional CSI path: update deploy/kubernetes/secret-provider-class.yaml and attach volumes."
Write-Host ""
Write-Host "Vault URI: https://$KeyVaultName.vault.azure.net/"
Write-Host "Docs: docs/KEYVAULT.md"
