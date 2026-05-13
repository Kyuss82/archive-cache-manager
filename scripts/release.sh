#!/usr/bin/env bash
# scripts/release.sh — build, tag, and publish a release on GitHub.
#
# Usage:   scripts/release.sh <tag>
# Example: scripts/release.sh v2.17.0
#
# Prereqs:
#   - working tree clean (commit your changes first)
#   - HISTORY.md contains a "## v<major>.<minor>" section for this tag
#   - thirdparty/Unbroken.LaunchBox.Plugins/12.8/Unbroken.LaunchBox.Plugins.dll present
#   - dotnet on PATH
#   - gh CLI authenticated (gh auth login)
set -euo pipefail

if [[ $# -ne 1 ]]; then
    echo "Usage: $0 <tag>   e.g. $0 v2.17.0" >&2
    exit 2
fi

TAG="$1"
if [[ ! "$TAG" =~ ^v[0-9]+\.[0-9]+(\.[0-9]+)?(-[A-Za-z0-9.-]+)?$ ]]; then
    echo "Error: tag '$TAG' does not look like a semver-ish version (expected vMAJOR.MINOR[.PATCH][-suffix])." >&2
    exit 2
fi

ROOT="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

# --- Sanity checks -----------------------------------------------------------

if [[ -n "$(git status --porcelain)" ]]; then
    echo "Error: working tree has uncommitted changes. Commit or stash them first." >&2
    git status --short >&2
    exit 1
fi

if git rev-parse -q --verify "refs/tags/$TAG" >/dev/null; then
    echo "Error: tag $TAG already exists locally." >&2
    exit 1
fi

if git ls-remote --tags origin "refs/tags/$TAG" | grep -q "$TAG"; then
    echo "Error: tag $TAG already exists on origin." >&2
    exit 1
fi

# Extract major.minor (e.g. v2.17.0 -> 2.17) and find the matching HISTORY section.
MAJOR_MINOR="${TAG#v}"
MAJOR_MINOR="${MAJOR_MINOR%%.*}.$(echo "${TAG#v}" | cut -d. -f2)"
HISTORY_HEADER="## v${MAJOR_MINOR}"

if ! grep -qF "$HISTORY_HEADER" HISTORY.md; then
    echo "Error: HISTORY.md has no section starting with '$HISTORY_HEADER'." >&2
    echo "Add a changelog entry for this release before tagging." >&2
    exit 1
fi

if ! command -v gh >/dev/null 2>&1; then
    echo "Error: gh CLI not found on PATH. Install: https://cli.github.com/" >&2
    exit 1
fi

if ! gh auth status >/dev/null 2>&1; then
    echo "Error: gh CLI not authenticated. Run: gh auth login" >&2
    exit 1
fi

# --- Build -------------------------------------------------------------------

echo ">> Building Release configuration..."
(cd src && dotnet build ArchiveCacheManager.sln -c Release --nologo -v minimal)

ZIP="release/ArchiveCacheManager.zip"
if [[ ! -f "$ZIP" ]]; then
    echo "Error: $ZIP not produced by build." >&2
    exit 1
fi
echo ">> Built: $ZIP ($(stat -c%s "$ZIP" 2>/dev/null || wc -c <"$ZIP") bytes)"

# --- Release notes -----------------------------------------------------------

# Extract the HISTORY section for this version (from "## vX.Y" up to the next "## ").
NOTES_FILE="$(mktemp)"
trap 'rm -f "$NOTES_FILE"' EXIT
awk -v hdr="$HISTORY_HEADER" '
    $0 ~ "^"hdr             { collecting=1; print; next }
    collecting && /^## /    { exit }
    collecting              { print }
' HISTORY.md > "$NOTES_FILE"

if [[ ! -s "$NOTES_FILE" ]]; then
    echo "Error: failed to extract release notes for $HISTORY_HEADER from HISTORY.md." >&2
    exit 1
fi

# --- Tag + push + release ----------------------------------------------------

echo ">> Tagging $TAG..."
git tag -a "$TAG" -m "$TAG"

echo ">> Pushing tag to origin..."
git push origin "$TAG"

echo ">> Creating GitHub release..."
gh release create "$TAG" "$ZIP" \
    --title "$TAG" \
    --notes-file "$NOTES_FILE"

echo
echo ">> Done."
gh release view "$TAG" --web 2>/dev/null || true
