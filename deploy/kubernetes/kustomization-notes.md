# Apply order for Mini Commerce on AKS

Full Azure resource checklist (SQL, Service Bus, APIM, ACR):  
[`../azure/AZURE-ARCHITECTURE.md`](../azure/AZURE-ARCHITECTURE.md)

## Before apply

1. Build and push images to ACR; update Deployment `image:` fields.
2. Prefer Key Vault CSI (`service-account.yaml` + `secret-provider-class.yaml`)  
   **or** `cp secrets.yaml.example secrets.yaml` and fill values (do not commit).
3. Edit `secret-provider-class.yaml` (client ID, vault name, tenant).
4. Set ConfigMap CORS origin to your storefront URL.

## Apply manifests

```bash
kubectl apply -f namespace.yaml
kubectl apply -f service-account.yaml
kubectl apply -f secret-provider-class.yaml
kubectl apply -f configmap.yaml
# kubectl apply -f secrets.yaml   # only if not using CSI sync yet

kubectl apply -f auth-service-deployment.yaml
kubectl apply -f catalog-service-deployment.yaml
kubectl apply -f cart-service-deployment.yaml
kubectl apply -f payment-service-deployment.yaml
kubectl apply -f inventory-service-deployment.yaml
kubectl apply -f notification-service-deployment.yaml
kubectl apply -f order-service-deployment.yaml

kubectl apply -f services.yaml
kubectl apply -f ingress.yaml
kubectl apply -f hpa.yaml
```

## Verify

```bash
kubectl get pods -n mini-commerce
kubectl get ingress -n mini-commerce
curl -s http://<INGRESS_IP>/api/catalog -H "Host: api.mini-commerce.local"
```
