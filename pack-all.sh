#!/usr/bin/env bash

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR"

CONFIGURATION="${CONFIGURATION:-Release}"
VERSION="${VERSION:-$(date +%Y%m%d)}"
OUTPUT_ROOT="${OUTPUT_ROOT:-$SCRIPT_DIR/dist}"
CLEAN="${CLEAN:-1}"

echo "========================================"
echo "  DHPlugin Full Packaging"
echo "========================================"
echo "Configuration: $CONFIGURATION"
echo "Version: $VERSION"
echo "Output root: $OUTPUT_ROOT"
echo ""

CONFIGURATION="$CONFIGURATION" OUTPUT_ROOT="$OUTPUT_ROOT" CLEAN="$CLEAN" "$SCRIPT_DIR/pack-plugin.sh"
CONFIGURATION="$CONFIGURATION" VERSION="$VERSION" OUTPUT_ROOT="$OUTPUT_ROOT" CLEAN="$CLEAN" "$SCRIPT_DIR/pack-host.sh"

echo ""
echo "========================================"
echo "All packaging completed"
echo "Plugins: $OUTPUT_ROOT/plugins"
echo "Host: $OUTPUT_ROOT/host"
echo "========================================"
