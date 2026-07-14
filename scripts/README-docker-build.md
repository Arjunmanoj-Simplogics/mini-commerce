# Mini Commerce — Docker build automation

Automation only: builds images from `deploy/docker/*.Dockerfile`. Does not modify application code or Dockerfiles.

## Folder structure

```text
VERSION                          # Semantic version source of truth (e.g. 1.0.0)
scripts/
  build-images.sh                # Discover, buildx build, tag, summarize
  tag-images.sh                  # Retag existing local images
  clean-images.sh                # Remove framework images / cache
  lib/
    docker-common.sh             # Shared logging, versioning, discovery
artifacts/docker/                # Created at build time
  build-summary.txt
  logs/<Service>.log
  results/<Service>.result
.docker-cache/buildx/            # Local BuildKit cache (optional; gitignored)
deploy/docker/*.Dockerfile       # Image definitions (unchanged by these scripts)
```

## Scripts

| Script | Purpose |
|--------|---------|
| `build-images.sh` | Discover Dockerfiles, `docker buildx build --pull --load`, parallel builds, retries, sizes, summary |
| `tag-images.sh` | Ensure `:semver`, `:git-sha`, `:latest` on already-built images |
| `clean-images.sh` | Delete `minicommerce/*` images; optional `--cache` / `--builder` |

## Usage

```bash
# Linux / Git Bash / CI
chmod +x scripts/*.sh scripts/lib/*.sh   # once

./scripts/build-images.sh
./scripts/tag-images.sh
./scripts/clean-images.sh --dry-run
./scripts/clean-images.sh --all
```

### Azure DevOps

```yaml
- bash: ./scripts/build-images.sh
  displayName: Build Docker images
  env:
    IMAGE_PREFIX: $(acrName).azurecr.io/minicommerce
    PARALLEL_JOBS: "3"
```

### GitHub Actions

```yaml
- name: Build images
  run: ./scripts/build-images.sh
  env:
    IMAGE_PREFIX: ghcr.io/org/minicommerce
```

## Tags produced

For each discovered Dockerfile (example `OrderService.Dockerfile`):

| Tag | Meaning |
|-----|---------|
| `minicommerce/order-service:1.0.0` | Semantic version from `VERSION` (or CI / git tag) |
| `minicommerce/order-service:<git-sha>` | Short commit SHA (12 chars) |
| `minicommerce/order-service:latest` | Rolling latest |

Image names are derived automatically: `AuthService.Dockerfile` → `minicommerce/auth-service`.

## Environment variables

| Variable | Default | Description |
|----------|---------|-------------|
| `IMAGE_PREFIX` | `minicommerce` | Registry/namespace prefix |
| `PARALLEL_JOBS` | min(CPU,4) | Concurrent build jobs |
| `BUILD_RETRIES` | `2` | Extra attempts after first failure |
| `DOCKER_PROGRESS` | `plain` | BuildKit progress mode |
| `CACHE_DIR` | `.docker-cache/buildx` | Local BuildKit cache path |
| `SKIP_PULL` | `0` | Set `1` to skip `--pull` |
| `NO_COLOR` | | Disable ANSI colors |
| `DEBUG` | | Verbose logs |

Does **not** push images and does **not** authenticate to Azure.
