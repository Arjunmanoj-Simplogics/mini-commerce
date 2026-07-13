<#
.SYNOPSIS
  Provisions Azure Service Bus namespace, topic, and subscriptions for Mini Commerce.

.PARAMETER ResourceGroup
  Existing Azure resource group name.

.PARAMETER NamespaceName
  Service Bus namespace (must be globally unique).

.PARAMETER Location
  Azure region (default: eastus).
#>
param(
    [Parameter(Mandatory = $true)]
    [string]$ResourceGroup,

    [Parameter(Mandatory = $true)]
    [string]$NamespaceName,

    [string]$Location = "eastus",

    [string]$TopicName = "orders",

    [string]$NotificationSubscription = "notification-service",

    [string]$InventorySubscription = "inventory-service"
)

$ErrorActionPreference = "Stop"

Write-Host "Ensuring resource group '$ResourceGroup' in '$Location'..."
az group create --name $ResourceGroup --location $Location | Out-Null

Write-Host "Creating Service Bus namespace '$NamespaceName' (Standard tier)..."
az servicebus namespace create `
    --resource-group $ResourceGroup `
    --name $NamespaceName `
    --sku Standard | Out-Null

Write-Host "Creating topic '$TopicName'..."
az servicebus topic create `
    --resource-group $ResourceGroup `
    --namespace-name $NamespaceName `
    --name $TopicName | Out-Null

Write-Host "Creating subscription '$NotificationSubscription'..."
az servicebus topic subscription create `
    --resource-group $ResourceGroup `
    --namespace-name $NamespaceName `
    --topic-name $TopicName `
    --name $NotificationSubscription | Out-Null

Write-Host "Creating subscription '$InventorySubscription' (for future use)..."
az servicebus topic subscription create `
    --resource-group $ResourceGroup `
    --namespace-name $NamespaceName `
    --topic-name $TopicName `
    --name $InventorySubscription | Out-Null

Write-Host "Fetching connection string..."
$connectionString = az servicebus namespace authorization-rule keys list `
    --resource-group $ResourceGroup `
    --namespace-name $NamespaceName `
    --name RootManageSharedAccessKey `
    --query primaryConnectionString `
    --output tsv

Write-Host ""
Write-Host "Provisioning complete."
Write-Host "Set these environment variables (do not commit secrets):"
Write-Host "  SERVICEBUS_ENABLED=true"
Write-Host "  SERVICEBUS_CONNECTION_STRING=$connectionString"
Write-Host ""
Write-Host "Or for local appsettings / user-secrets:"
Write-Host "  ServiceBus__Enabled=true"
Write-Host "  ServiceBus__ConnectionString=<connection-string>"
Write-Host "  ServiceBus__TopicName=$TopicName"
Write-Host "  ServiceBus__SubscriptionName=$NotificationSubscription  (Notification Service only)"
