#!/usr/bin/env bash
# =============================================================================
# Mini Commerce — remove locally built framework images and optional caches
#
# Usage:
#   ./scripts/clean-images.sh
#   ./scripts/clean-images.sh --cache
#   ./scripts/clean-images.sh --all
#   ./scripts/clean-images.sh --dry-run
#
# Discovers image repositories from deploy/docker Dockerfiles and removes
# matching local tags (${IMAGE_PREFIX}/...).
#
# Does NOT touch unrelated Docker images.
# Does NOT push / pull / login.
# =============================================================================

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=lib/docker-common.sh
source "${SCRIPT_DIR}/lib/docker-common.sh"

ROOT="$(cd "${SCRIPT_DIR}/.." && pwd)"
CLEAN_CACHE=0
CLEAN_BUILDER=0
DRY_RUN=0

usage() {
  cat <<EOF
Usage: $(basename "$0") [options]

  --cache       Also delete local BuildKit cache directory (.docker-cache/buildx)
  --builder     Also remove the dedicated buildx builder (${BUILDX_BUILDER})
  --all         Equivalent to --cache --builder
  --dry-run     Print actions without deleting
  -h, --help    Show this help

Environment:
  IMAGE_PREFIX  Image namespace to clean (default: minicommerce)
EOF
}

parse_args() {
  while [[ $# -gt 0 ]]; do
    case "$1" in
      --cache) CLEAN_CACHE=1; shift ;;
      --builder) CLEAN_BUILDER=1; shift ;;
      --all) CLEAN_CACHE=1; CLEAN_BUILDER=1; shift ;;
      --dry-run) DRY_RUN=1; shift ;;
      -h|--help) usage; exit 0 ;;
      *) die "Unknown argument: $1" ;;
    esac
  done
}

# -----------------------------------------------------------------------------
# list_local_tags_for_repo: all local tags for a repository name
# -----------------------------------------------------------------------------
list_local_tags_for_repo() {
  local repo="$1"
  # docker images format: Repository:Tag for matching repository
  docker images --format '{{.Repository}}:{{.Tag}}' \
    | awk -v r="$repo" '$0 ~ "^"r":" { print }' \
    | grep -v '<none>' || true
}

# -----------------------------------------------------------------------------
# remove_image_ref
# -----------------------------------------------------------------------------
remove_image_ref() {
  local ref="$1"
  if (( DRY_RUN )); then
    log_info "[dry-run] docker rmi -f ${ref}"
    return 0
  fi
  if docker rmi -f "$ref" >/dev/null 2>&1; then
    log_ok "Removed ${ref}"
  else
    log_warn "Could not remove ${ref} (in use or already gone)"
  fi
}

# -----------------------------------------------------------------------------
# clean_repositories
# -----------------------------------------------------------------------------
clean_repositories() {
  mapfile -t DOCKERFILES < <(discover_dockerfiles "$ROOT")
  ((${#DOCKERFILES[@]} > 0)) || die "No Dockerfiles under ${DOCKER_DIR}"

  local df repo tags tag removed=0
  declare -a all_refs=()

  for df in "${DOCKERFILES[@]}"; do
    repo="$(image_repository "$df")"
    log_step "Scanning local tags for ${repo}"
    mapfile -t tags < <(list_local_tags_for_repo "$repo")
    if ((${#tags[@]} == 0)); then
      log_info "  (none)"
      continue
    fi
    for tag in "${tags[@]}"; do
      log_info "  found ${tag}"
      all_refs+=("$tag")
    done
  done

  if ((${#all_refs[@]} == 0)); then
    log_warn "No local ${IMAGE_PREFIX}/* images found to clean"
    return 0
  fi

  log_step "Removing ${#all_refs[@]} image reference(s)"
  for tag in "${all_refs[@]}"; do
    remove_image_ref "$tag"
    removed=$((removed + 1))
  done

  log_ok "Processed ${removed} image reference(s)"
}

# -----------------------------------------------------------------------------
# clean_cache_dir
# -----------------------------------------------------------------------------
clean_cache_dir() {
  local cache
  cache="$(default_cache_dir "$ROOT")"
  if [[ ! -d "$cache" ]]; then
    log_info "Cache directory not present: ${cache}"
    return 0
  fi
  if (( DRY_RUN )); then
    log_info "[dry-run] rm -rf ${cache}"
    return 0
  fi
  log_step "Removing BuildKit cache: ${cache}"
  rm -rf "$cache"
  log_ok "Cache removed"
}

# -----------------------------------------------------------------------------
# clean_builder
# -----------------------------------------------------------------------------
clean_builder() {
  if (( DRY_RUN )); then
    log_info "[dry-run] docker buildx rm ${BUILDX_BUILDER}"
    return 0
  fi
  if docker buildx inspect "$BUILDX_BUILDER" >/dev/null 2>&1; then
    log_step "Removing buildx builder: ${BUILDX_BUILDER}"
    docker buildx rm -f "$BUILDX_BUILDER" >/dev/null 2>&1 || log_warn "Could not remove builder ${BUILDX_BUILDER}"
    log_ok "Builder removed"
  else
    log_info "Builder not found: ${BUILDX_BUILDER}"
  fi
}

# -----------------------------------------------------------------------------
# main
# -----------------------------------------------------------------------------
main() {
  parse_args "$@"
  cd "$ROOT"

  log_step "Mini Commerce image cleanup"
  require_docker

  log_info "Image prefix: ${IMAGE_PREFIX}"
  log_info "Dry run:      ${DRY_RUN}"

  clean_repositories

  if (( CLEAN_CACHE )); then
    clean_cache_dir
  fi

  if (( CLEAN_BUILDER )); then
    clean_builder
  fi

  # Best-effort dangling cleanup for our builds only when not dry-run
  if (( ! DRY_RUN )); then
    log_info "Pruning dangling images (docker image prune -f)"
    docker image prune -f >/dev/null 2>&1 || true
  else
    log_info "[dry-run] docker image prune -f"
  fi

  log_ok "Cleanup complete"
}

main "$@"
