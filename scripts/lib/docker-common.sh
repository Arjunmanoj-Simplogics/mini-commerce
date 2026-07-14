#!/usr/bin/env bash
# =============================================================================
# Mini Commerce — shared Docker build framework helpers
# Sourced by: build-images.sh, tag-images.sh, clean-images.sh
# =============================================================================

# Prevent double-source
if [[ -n "${_MINICOMMERCE_DOCKER_COMMON_LOADED:-}" ]]; then
  return 0 2>/dev/null || true
fi
_MINICOMMERCE_DOCKER_COMMON_LOADED=1

# Fail fast (callers may temporarily disable with set +e)
set -o pipefail

# ---------------------------------------------------------------------------
# Paths & defaults
# ---------------------------------------------------------------------------

script_dir() {
  local src="${BASH_SOURCE[1]:-${BASH_SOURCE[0]}}"
  cd "$(dirname "$src")" && pwd
}

# Repository root (parent of scripts/)
repo_root() {
  local src d
  src="${BASH_SOURCE[1]:-${BASH_SOURCE[0]}}"
  d="$(cd "$(dirname "$src")/.." && pwd)"
  echo "$d"
}

: "${IMAGE_PREFIX:=minicommerce}"
: "${DOCKER_DIR:=deploy/docker}"
: "${VERSION_FILE:=VERSION}"
: "${BUILDX_BUILDER:=minicommerce-builder}"
: "${PARALLEL_JOBS:=}"
: "${BUILD_RETRIES:=2}"
: "${DOCKER_PROGRESS:=plain}"
: "${CACHE_DIR:=}"
: "${NO_COLOR:=}"

# ---------------------------------------------------------------------------
# Logging (enterprise, color-aware)
# ---------------------------------------------------------------------------

_supports_color() {
  if [[ -n "${NO_COLOR:-}" ]]; then
    return 1
  fi
  if [[ -n "${GITHUB_ACTIONS:-}" || -n "${TF_BUILD:-}" || -n "${AZURE_HTTP_USER_AGENT:-}" ]]; then
    # CI consoles often support ANSI; keep colors unless NO_COLOR
    return 0
  fi
  [[ -t 1 ]]
}

if _supports_color; then
  C_RESET=$'\033[0m'
  C_BOLD=$'\033[1m'
  C_DIM=$'\033[2m'
  C_RED=$'\033[31m'
  C_GREEN=$'\033[32m'
  C_YELLOW=$'\033[33m'
  C_BLUE=$'\033[34m'
  C_CYAN=$'\033[36m'
else
  C_RESET= C_BOLD= C_DIM= C_RED= C_GREEN= C_YELLOW= C_BLUE= C_CYAN=
fi

log_ts() { date -u +"%Y-%m-%dT%H:%M:%SZ"; }

log_info()  { printf '%s %s[INFO ]%s %s\n' "$(log_ts)" "${C_BLUE}"   "${C_RESET}" "$*"; }
log_ok()    { printf '%s %s[ OK  ]%s %s\n' "$(log_ts)" "${C_GREEN}"  "${C_RESET}" "$*"; }
log_warn()  { printf '%s %s[WARN ]%s %s\n' "$(log_ts)" "${C_YELLOW}" "${C_RESET}" "$*" >&2; }
log_error() { printf '%s %s[ERROR]%s %s\n' "$(log_ts)" "${C_RED}"    "${C_RESET}" "$*" >&2; }
log_step()  { printf '%s %s[STEP ]%s %s%s%s\n' "$(log_ts)" "${C_CYAN}" "${C_RESET}" "${C_BOLD}" "$*" "${C_RESET}"; }
log_debug() {
  if [[ "${DEBUG:-0}" == "1" || "${VERBOSE:-0}" == "1" ]]; then
    printf '%s %s[DEBUG]%s %s\n' "$(log_ts)" "${C_DIM}" "${C_RESET}" "$*"
  fi
}

die() {
  log_error "$*"
  exit 1
}

# ---------------------------------------------------------------------------
# Prerequisites
# ---------------------------------------------------------------------------

require_cmd() {
  command -v "$1" >/dev/null 2>&1 || die "Required command not found: $1"
}

require_docker() {
  require_cmd docker
  docker info >/dev/null 2>&1 || die "Docker daemon is not running or not reachable"
  docker buildx version >/dev/null 2>&1 || die "docker buildx is required — install Docker BuildKit / buildx"
}

# ---------------------------------------------------------------------------
# Versioning
# ---------------------------------------------------------------------------

# Reads semantic version from VERSION file, CI vars, git describe, or default.
read_project_version() {
  local root="$1"
  local ver=""

  if [[ -f "${root}/${VERSION_FILE}" ]]; then
    ver="$(tr -d '[:space:]' < "${root}/${VERSION_FILE}")"
  fi

  # Prefer CI-provided numbers when VERSION looks like a base and BUILD_BUILDNUMBER is set
  if [[ -n "${BUILD_BUILDNUMBER:-}" ]]; then
    # Azure DevOps: often "20260714.1" — if VERSION is semver, append build as patch metadata is avoided;
    # use BUILD_BUILDNUMBER only when VERSION file is missing
    if [[ -z "$ver" ]]; then
      ver="${BUILD_BUILDNUMBER}"
    fi
  fi

  if [[ -z "$ver" && -n "${GITHUB_REF_NAME:-}" && "${GITHUB_REF:-}" == refs/tags/* ]]; then
    ver="${GITHUB_REF_NAME#v}"
  fi

  if [[ -z "$ver" ]] && command -v git >/dev/null 2>&1; then
    ver="$(git -C "$root" describe --tags --abbrev=0 2>/dev/null | sed 's/^v//' || true)"
  fi

  if [[ -z "$ver" ]]; then
    ver="0.0.0"
    log_warn "No VERSION file / CI / git tag found — using ${ver}"
  fi

  # Normalize: strip leading v
  ver="${ver#v}"
  echo "$ver"
}

# Short Git commit SHA (12 chars); "nogit" when unavailable.
read_git_sha() {
  local root="$1"
  if command -v git >/dev/null 2>&1 && git -C "$root" rev-parse --is-inside-work-tree >/dev/null 2>&1; then
    git -C "$root" rev-parse --short=12 HEAD 2>/dev/null || echo "nogit"
  else
    echo "nogit"
  fi
}

# ---------------------------------------------------------------------------
# Dockerfile discovery & image naming
# ---------------------------------------------------------------------------

# Lists absolute paths of *.Dockerfile under deploy/docker (sorted).
discover_dockerfiles() {
  local root="$1"
  local dir="${root}/${DOCKER_DIR}"
  [[ -d "$dir" ]] || die "Docker directory not found: ${dir}"

  local f
  # Portable: find + sort (Git Bash / Linux / CI)
  while IFS= read -r f; do
    [[ -n "$f" ]] && printf '%s\n' "$f"
  done < <(find "$dir" -maxdepth 1 -type f -name '*.Dockerfile' 2>/dev/null | LC_ALL=C sort)
}

# AuthService.Dockerfile → AuthService ; Frontend.Dockerfile → Frontend
dockerfile_stem() {
  local base
  base="$(basename "$1")"
  echo "${base%.Dockerfile}"
}

# PascalCase / CamelCase → kebab-case (AuthService → auth-service)
pascal_to_kebab() {
  echo "$1" | sed -E 's/([a-z0-9])([A-Z])/\1-\2/g' | tr '[:upper:]' '[:lower:]'
}

# Full image repository without tag: minicommerce/auth-service
image_repository() {
  local dockerfile="$1"
  local stem kebab
  stem="$(dockerfile_stem "$dockerfile")"
  kebab="$(pascal_to_kebab "$stem")"
  echo "${IMAGE_PREFIX}/${kebab}"
}

# Build context directory for a Dockerfile
build_context_for() {
  local root="$1"
  local dockerfile="$2"
  local stem
  stem="$(dockerfile_stem "$dockerfile")"
  case "$stem" in
    Frontend|frontend)
      echo "${root}/frontend"
      ;;
    *)
      echo "${root}"
      ;;
  esac
}

# Tags for an image: semver, git sha, latest
image_tags() {
  local repo="$1"
  local semver="$2"
  local sha="$3"
  printf '%s\n' \
    "${repo}:${semver}" \
    "${repo}:${sha}" \
    "${repo}:latest"
}

# ---------------------------------------------------------------------------
# Duration / size helpers
# ---------------------------------------------------------------------------

now_epoch() {
  date +%s
}

format_duration() {
  local secs="${1:-0}"
  local h=$((secs / 3600))
  local m=$(((secs % 3600) / 60))
  local s=$((secs % 60))
  if (( h > 0 )); then
    printf '%dh%02dm%02ds' "$h" "$m" "$s"
  elif (( m > 0 )); then
    printf '%dm%02ds' "$m" "$s"
  else
    printf '%ds' "$s"
  fi
}

# Human-readable image size for a local tag (best-effort)
image_size_human() {
  local ref="$1"
  docker image inspect "$ref" --format '{{.Size}}' 2>/dev/null | awk '{
    split("B KB MB GB TB", u);
    s=$1;
    for (i=1; s>=1024 && i<5; i++) s/=1024;
    printf "%.2f %s", s, u[i];
  }' || echo "n/a"
}

# ---------------------------------------------------------------------------
# Buildx builder
# ---------------------------------------------------------------------------

ensure_buildx_builder() {
  log_step "Ensuring buildx builder '${BUILDX_BUILDER}'"
  export DOCKER_BUILDKIT=1
  export BUILDX_NO_DEFAULT_ATTESTATIONS="${BUILDX_NO_DEFAULT_ATTESTATIONS:-1}"

  if ! docker buildx inspect "$BUILDX_BUILDER" >/dev/null 2>&1; then
    log_info "Creating buildx builder: ${BUILDX_BUILDER}"
    if ! docker buildx create --name "$BUILDX_BUILDER" --driver docker-container --use >/dev/null 2>&1; then
      log_warn "docker-container driver unavailable — using default builder"
      docker buildx use default >/dev/null 2>&1 || true
      BUILDX_BUILDER="default"
      return 0
    fi
    docker buildx inspect --bootstrap >/dev/null 2>&1 || true
  else
    docker buildx use "$BUILDX_BUILDER" >/dev/null 2>&1 || docker buildx use default >/dev/null 2>&1 || true
  fi
}

# True when builder supports local cache export
buildx_supports_local_cache() {
  local driver
  driver="$(docker buildx inspect "$BUILDX_BUILDER" 2>/dev/null | awk -F': ' '/Driver:/ {print $2; exit}' | tr -d '[:space:]')"
  [[ "$driver" == "docker-container" || "$driver" == "kubernetes" || "$driver" == "remote" ]]
}

default_cache_dir() {
  local root="$1"
  if [[ -n "${CACHE_DIR}" ]]; then
    echo "$CACHE_DIR"
  else
    echo "${root}/.docker-cache/buildx"
  fi
}

# ---------------------------------------------------------------------------
# Parallelism
# ---------------------------------------------------------------------------

detect_parallel_jobs() {
  if [[ -n "${PARALLEL_JOBS}" ]]; then
    echo "$PARALLEL_JOBS"
    return
  fi
  local n
  n="$(getconf _NPROCESSORS_ONLN 2>/dev/null || nproc 2>/dev/null || sysctl -n hw.ncpu 2>/dev/null || echo 2)"
  # Cap default concurrency to avoid thrashing local Docker Desktop
  if (( n > 4 )); then
    echo 4
  else
    echo "$n"
  fi
}

# Wait for up to $1 background jobs (job count running)
wait_for_slot() {
  local max="$1"
  while true; do
    local running
    running="$(jobs -rp | wc -l | tr -d ' ')"
    if (( running < max )); then
      break
    fi
    sleep 1
  done
}
