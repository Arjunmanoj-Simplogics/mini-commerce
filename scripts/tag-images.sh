#!/usr/bin/env bash
# =============================================================================
# Mini Commerce — (re)tag built images with semantic version, Git SHA, latest
#
# Usage:
#   ./scripts/tag-images.sh
#   ./scripts/tag-images.sh --from-tag latest
#   PROJECT_VERSION=1.2.3 ./scripts/tag-images.sh
#
# Discovers Dockerfiles under deploy/docker, derives image repository names,
# and ensures each local image has:
#   :{semver}  :{git-sha}  :latest
#
# Does NOT build. Does NOT push.
# =============================================================================

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=lib/docker-common.sh
source "${SCRIPT_DIR}/lib/docker-common.sh"

ROOT="$(cd "${SCRIPT_DIR}/.." && pwd)"
FROM_TAG="latest"

usage() {
  cat <<EOF
Usage: $(basename "$0") [--from-tag TAG] [-h|--help]

  --from-tag TAG   Source tag already present locally (default: latest)
  -h, --help       Show this help

Reads project version from VERSION / CI / git (see docker-common.sh).
EOF
}

parse_args() {
  while [[ $# -gt 0 ]]; do
    case "$1" in
      --from-tag)
        FROM_TAG="${2:-}"
        [[ -n "$FROM_TAG" ]] || die "--from-tag requires a value"
        shift 2
        ;;
      -h|--help)
        usage
        exit 0
        ;;
      *)
        die "Unknown argument: $1"
        ;;
    esac
  done
}

# -----------------------------------------------------------------------------
# image_exists: true if local docker image reference exists
# -----------------------------------------------------------------------------
image_exists() {
  docker image inspect "$1" >/dev/null 2>&1
}

# -----------------------------------------------------------------------------
# tag_image_ref: docker tag with retry
# -----------------------------------------------------------------------------
tag_image_ref() {
  local source="$1"
  local target="$2"
  local attempt=1
  local max=$((BUILD_RETRIES + 1))

  while (( attempt <= max )); do
    if docker tag "$source" "$target" 2>/dev/null; then
      log_ok "Tagged ${target}"
      return 0
    fi
    log_warn "docker tag failed (${attempt}/${max}): ${source} → ${target}"
    sleep $((attempt * 2))
    attempt=$((attempt + 1))
  done
  log_error "Could not tag ${target}"
  return 1
}

# -----------------------------------------------------------------------------
# resolve_source: find a local image to retag (prefer --from-tag, then semver, sha)
# -----------------------------------------------------------------------------
resolve_source() {
  local repo="$1"
  local semver="$2"
  local sha="$3"
  local candidate

  for candidate in "${repo}:${FROM_TAG}" "${repo}:${semver}" "${repo}:${sha}" "${repo}:latest"; do
    if image_exists "$candidate"; then
      echo "$candidate"
      return 0
    fi
  done
  return 1
}

# -----------------------------------------------------------------------------
# tag_one_repository
# -----------------------------------------------------------------------------
tag_one_repository() {
  local repo="$1"
  local semver="$2"
  local sha="$3"
  local source

  if ! source="$(resolve_source "$repo" "$semver" "$sha")"; then
    log_warn "No local image found for ${repo} (looked for :${FROM_TAG}, :${semver}, :${sha}, :latest) — skip"
    return 1
  fi

  log_step "Tagging ${repo} (source=${source})"
  local tag rc=0
  while IFS= read -r tag; do
    if [[ "$tag" == "$source" ]]; then
      log_info "Already tagged: ${tag}"
      continue
    fi
    if ! tag_image_ref "$source" "$tag"; then
      rc=1
    fi
  done < <(image_tags "$repo" "$semver" "$sha")

  return "$rc"
}

# -----------------------------------------------------------------------------
# main
# -----------------------------------------------------------------------------
main() {
  parse_args "$@"
  cd "$ROOT"

  log_step "Mini Commerce image tagging"
  require_docker

  local semver sha
  semver="$(read_project_version "$ROOT")"
  sha="$(read_git_sha "$ROOT")"

  # Allow override without editing VERSION
  if [[ -n "${PROJECT_VERSION:-}" ]]; then
    semver="${PROJECT_VERSION#v}"
  fi

  log_info "Semantic version: ${semver}"
  log_info "Git SHA:          ${sha}"
  log_info "Source tag hint:  ${FROM_TAG}"
  log_info "Image prefix:     ${IMAGE_PREFIX}"

  mapfile -t DOCKERFILES < <(discover_dockerfiles "$ROOT")
  ((${#DOCKERFILES[@]} > 0)) || die "No Dockerfiles under ${DOCKER_DIR}"

  local ok=0 fail=0 df repo
  declare -a tagged=()
  declare -a skipped=()

  for df in "${DOCKERFILES[@]}"; do
    repo="$(image_repository "$df")"
    if tag_one_repository "$repo" "$semver" "$sha"; then
      ok=$((ok + 1))
      tagged+=("$repo")
    else
      fail=$((fail + 1))
      skipped+=("$repo")
    fi
  done

  echo
  log_step "Tagging summary"
  log_info "Tagged successfully: ${ok}"
  if ((${#tagged[@]} > 0)); then
    for repo in "${tagged[@]}"; do
      log_ok "  ${repo}:{${semver},${sha},latest}"
    done
  fi
  if (( fail > 0 )); then
    log_warn "Skipped / failed: ${fail}"
    if ((${#skipped[@]} > 0)); then
      for repo in "${skipped[@]}"; do
        log_warn "  ${repo}"
      done
    fi
  fi

  if (( fail > 0 && ok == 0 )); then
    die "No images were tagged — run ./scripts/build-images.sh first"
  fi

  log_ok "Tagging complete"
}

main "$@"
