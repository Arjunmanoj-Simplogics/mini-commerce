#!/usr/bin/env bash
# =============================================================================
# Mini Commerce — build all images under deploy/docker with docker buildx
#
# Usage:
#   ./scripts/build-images.sh
#   PARALLEL_JOBS=2 ./scripts/build-images.sh
#   IMAGE_PREFIX=myacr.azurecr.io/minicommerce ./scripts/build-images.sh
#
# Environment:
#   IMAGE_PREFIX     Image namespace (default: minicommerce)
#   PARALLEL_JOBS    Concurrent builds (default: min(CPU,4))
#   BUILD_RETRIES    Retries per image on failure (default: 2)
#   DOCKER_PROGRESS  BuildKit progress: plain|tty|auto (default: plain)
#   CACHE_DIR        Local BuildKit cache directory
#   SKIP_PULL=1      Skip --pull of base images
#   NO_COLOR=1       Disable ANSI colors
#   DEBUG=1          Verbose debug logs
#
# Does NOT push images. Does NOT authenticate to Azure.
# =============================================================================

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=lib/docker-common.sh
source "${SCRIPT_DIR}/lib/docker-common.sh"

ROOT="$(cd "${SCRIPT_DIR}/.." && pwd)"
ARTIFACTS_DIR="${ROOT}/artifacts/docker"
SUMMARY_FILE="${ARTIFACTS_DIR}/build-summary.txt"
RESULTS_DIR="${ARTIFACTS_DIR}/results"
LOG_DIR="${ARTIFACTS_DIR}/logs"

mkdir -p "$ARTIFACTS_DIR" "$RESULTS_DIR" "$LOG_DIR"

declare -a SUCCESS_IMAGES=()
declare -a FAILED_IMAGES=()
declare -a SUCCESS_SIZES=()
declare -a SUCCESS_DURATIONS=()

BUILD_START=0
BUILD_END=0

# -----------------------------------------------------------------------------
# retry: run a command up to N times with backoff
# -----------------------------------------------------------------------------
retry() {
  local attempts="$1"
  shift
  local n=1
  local delay=3
  until "$@"; do
    if (( n >= attempts )); then
      return 1
    fi
    log_warn "Attempt ${n}/${attempts} failed — retrying in ${delay}s…"
    sleep "$delay"
    delay=$((delay * 2))
    n=$((n + 1))
  done
}

# -----------------------------------------------------------------------------
# build_one_image: buildx build + load + multi-tag for a single Dockerfile
# -----------------------------------------------------------------------------
build_one_image() {
  local dockerfile="$1"
  local semver="$2"
  local sha="$3"
  local cache_dir="$4"

  local repo stem context log_file result_file start end duration
  local -a tag_args=()
  local tag

  stem="$(dockerfile_stem "$dockerfile")"
  repo="$(image_repository "$dockerfile")"
  context="$(build_context_for "$ROOT" "$dockerfile")"

  if [[ ! -d "$context" ]]; then
    log_error "Build context missing for ${stem}: ${context}"
    echo "FAIL|${repo}|context-missing|0" > "${RESULTS_DIR}/${stem}.result"
    return 1
  fi

  log_file="${LOG_DIR}/${stem}.log"
  result_file="${RESULTS_DIR}/${stem}.result"

  log_step "Building ${C_BOLD}${repo}${C_RESET} from $(basename "$dockerfile")"
  log_info "Context: ${context}"
  log_info "Tags: ${semver}, ${sha}, latest"

  while IFS= read -r tag; do
    tag_args+=(--tag "$tag")
  done < <(image_tags "$repo" "$semver" "$sha")

  local -a build_cmd=(
    docker buildx build
    --builder "$BUILDX_BUILDER"
    --file "$dockerfile"
    --load
    --progress="$DOCKER_PROGRESS"
    --build-arg "BUILD_VERSION=${semver}"
    --build-arg "GIT_SHA=${sha}"
  )

  if [[ "${SKIP_PULL:-0}" != "1" ]]; then
    build_cmd+=(--pull)
  fi

  if buildx_supports_local_cache; then
    mkdir -p "$cache_dir"
    build_cmd+=(--cache-from "type=local,src=${cache_dir}")
    build_cmd+=(--cache-to "type=local,dest=${cache_dir},mode=max")
  else
    log_debug "Local cache export not supported on this builder — building without --cache-to"
  fi

  build_cmd+=("${tag_args[@]}")
  build_cmd+=("$context")

  start="$(now_epoch)"
  set +e
  retry "$((BUILD_RETRIES + 1))" "${build_cmd[@]}" >"$log_file" 2>&1
  local rc=$?
  set -e
  end="$(now_epoch)"
  duration=$((end - start))

  if (( rc == 0 )); then
    local size
    size="$(image_size_human "${repo}:${semver}")"
    log_ok "Built ${repo}:${semver} (${size}) in $(format_duration "$duration")"
    echo "OK|${repo}|${size}|${duration}" >"$result_file"
    return 0
  fi

  log_error "Failed ${repo} after $(format_duration "$duration") — see ${log_file}"
  # Show last lines of log for CI visibility
  tail -n 40 "$log_file" >&2 || true
  echo "FAIL|${repo}|failed|${duration}" >"$result_file"
  return 1
}

# -----------------------------------------------------------------------------
# collect_results: aggregate per-image result files after parallel builds
# -----------------------------------------------------------------------------
collect_results() {
  SUCCESS_IMAGES=()
  FAILED_IMAGES=()
  SUCCESS_SIZES=()
  SUCCESS_DURATIONS=()

  local f status repo size duration
  for f in "${RESULTS_DIR}"/*.result; do
    [[ -f "$f" ]] || continue
    IFS='|' read -r status repo size duration <"$f"
    if [[ "$status" == "OK" ]]; then
      SUCCESS_IMAGES+=("$repo")
      SUCCESS_SIZES+=("$size")
      SUCCESS_DURATIONS+=("$duration")
    else
      FAILED_IMAGES+=("$repo")
    fi
  done
}

# -----------------------------------------------------------------------------
# print_summary: final console + artifacts report
# -----------------------------------------------------------------------------
print_summary() {
  local total elapsed i
  total=$(( ${#SUCCESS_IMAGES[@]} + ${#FAILED_IMAGES[@]} ))
  elapsed=$((BUILD_END - BUILD_START))

  {
    echo "==============================================================================="
    echo " Mini Commerce — Docker Build Summary"
    echo "==============================================================================="
    echo " Timestamp (UTC): $(log_ts)"
    echo " Version:         ${PROJECT_VERSION}"
    echo " Git SHA:         ${GIT_SHA}"
    echo " Image prefix:    ${IMAGE_PREFIX}"
    echo " Duration:        $(format_duration "$elapsed")"
    echo " Parallel jobs:   ${JOBS}"
    echo " Builder:         ${BUILDX_BUILDER}"
    echo "-------------------------------------------------------------------------------"
    echo " Successful (${#SUCCESS_IMAGES[@]}/${total})"
    if ((${#SUCCESS_IMAGES[@]} > 0)); then
      for i in "${!SUCCESS_IMAGES[@]}"; do
        printf '   ✓ %-45s  %10s  %8s\n' \
          "${SUCCESS_IMAGES[$i]}" \
          "${SUCCESS_SIZES[$i]}" \
          "$(format_duration "${SUCCESS_DURATIONS[$i]}")"
      done
    else
      echo "   (none)"
    fi
    echo "-------------------------------------------------------------------------------"
    echo " Failed (${#FAILED_IMAGES[@]}/${total})"
    if ((${#FAILED_IMAGES[@]} > 0)); then
      for i in "${!FAILED_IMAGES[@]}"; do
        printf '   ✗ %s\n' "${FAILED_IMAGES[$i]}"
      done
    else
      echo "   (none)"
    fi
    echo "-------------------------------------------------------------------------------"
    echo " Tags applied per image:"
    echo "   :${PROJECT_VERSION}   (semantic version from VERSION / CI / git)"
    echo "   :${GIT_SHA}           (git commit)"
    echo "   :latest"
    echo "-------------------------------------------------------------------------------"
    echo " Logs:    ${LOG_DIR}"
    echo " Summary: ${SUMMARY_FILE}"
    echo "==============================================================================="
  } | tee "$SUMMARY_FILE"

  # Colored echo of same summary for terminals
  log_step "Build summary"
  log_info "Version=${PROJECT_VERSION} SHA=${GIT_SHA} Duration=$(format_duration "$elapsed")"
  if ((${#SUCCESS_IMAGES[@]} > 0)); then
    log_ok "Successful builds (${#SUCCESS_IMAGES[@]})"
    for i in "${!SUCCESS_IMAGES[@]}"; do
      log_ok "  ${SUCCESS_IMAGES[$i]}  size=${SUCCESS_SIZES[$i]}  time=$(format_duration "${SUCCESS_DURATIONS[$i]}")"
    done
  fi
  if ((${#FAILED_IMAGES[@]} > 0)); then
    log_error "Failed builds (${#FAILED_IMAGES[@]})"
    for i in "${!FAILED_IMAGES[@]}"; do
      log_error "  ${FAILED_IMAGES[$i]}"
    done
  fi
}

# -----------------------------------------------------------------------------
# main
# -----------------------------------------------------------------------------
main() {
  cd "$ROOT"

  log_step "Mini Commerce Docker build framework"
  require_docker

  PROJECT_VERSION="$(read_project_version "$ROOT")"
  GIT_SHA="$(read_git_sha "$ROOT")"
  JOBS="$(detect_parallel_jobs)"
  CACHE_PATH="$(default_cache_dir "$ROOT")"
  mkdir -p "$CACHE_PATH"

  # Fresh result slate
  rm -f "${RESULTS_DIR}"/*.result 2>/dev/null || true

  log_info "Repository:     ${ROOT}"
  log_info "Dockerfiles:    ${ROOT}/${DOCKER_DIR}"
  log_info "Project version:${PROJECT_VERSION}"
  log_info "Git SHA:        ${GIT_SHA}"
  log_info "Image prefix:   ${IMAGE_PREFIX}"
  log_info "Parallel jobs:  ${JOBS}"
  log_info "Build retries:  ${BUILD_RETRIES}"
  log_info "Cache dir:      ${CACHE_PATH}"
  log_info "Progress:       ${DOCKER_PROGRESS}"

  mapfile -t DOCKERFILES < <(discover_dockerfiles "$ROOT")
  if ((${#DOCKERFILES[@]} == 0)); then
    die "No *.Dockerfile found under ${ROOT}/${DOCKER_DIR}"
  fi

  log_info "Discovered ${#DOCKERFILES[@]} Dockerfile(s):"
  local df
  for df in "${DOCKERFILES[@]}"; do
    log_info "  - $(basename "$df") → $(image_repository "$df")"
  done

  ensure_buildx_builder

  BUILD_START="$(now_epoch)"

  log_step "Building images (parallel ≤ ${JOBS})"

  local pids=()
  local stem
  for df in "${DOCKERFILES[@]}"; do
    wait_for_slot "$JOBS"
    stem="$(dockerfile_stem "$df")"
    (
      # Isolate job failures from set -e parent
      set +e
      build_one_image "$df" "$PROJECT_VERSION" "$GIT_SHA" "$CACHE_PATH"
      exit $?
    ) &
    pids+=("$!")
    log_debug "Started job PID $! for ${stem}"
  done

  local pid rc=0
  for pid in "${pids[@]}"; do
    if ! wait "$pid"; then
      rc=1
    fi
  done

  BUILD_END="$(now_epoch)"
  collect_results
  print_summary

  if ((${#FAILED_IMAGES[@]} > 0)); then
    die "Build finished with ${#FAILED_IMAGES[@]} failure(s)"
  fi

  log_ok "All ${#SUCCESS_IMAGES[@]} image(s) built successfully"
  exit 0
}

main "$@"
