#!/usr/bin/env bash

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR"

CONFIGURATION="${CONFIGURATION:-Release}"
OUTPUT_ROOT="${OUTPUT_ROOT:-$SCRIPT_DIR/dist}"
PLUGIN_OUTPUT_DIR="$OUTPUT_ROOT/plugins"
CLEAN="${CLEAN:-1}"

if [[ "$CONFIGURATION" != "Release" ]]; then
    echo "ERROR: pack-plugin.sh requires CONFIGURATION=Release to generate .dhplugin packages."
    exit 1
fi

if [[ "$CLEAN" == "1" ]]; then
    rm -rf "$PLUGIN_OUTPUT_DIR"
fi
mkdir -p "$PLUGIN_OUTPUT_DIR"

PLUGIN_PROJECTS="$(find src/Plugins -mindepth 2 -maxdepth 2 -name "*.csproj" -type f | sort)"

if [[ -z "$PLUGIN_PROJECTS" ]]; then
    echo "ERROR: no plugin projects found under src/Plugins."
    exit 1
fi

echo "========================================"
echo "  DHPlugin Plugin Packaging"
echo "========================================"
echo "Configuration: $CONFIGURATION"
echo "Output: $PLUGIN_OUTPUT_DIR"
echo ""

PACKED_COUNT=0

while IFS= read -r project; do
    project_dir="$(dirname "$project")"
    project_name="$(basename "${project%.csproj}")"

    echo "----------------------------------------"
    echo "Building plugin: $project_name"
    echo "Project: $project"

    dotnet build "$project" -c "$CONFIGURATION"

    package_path=""
    output_candidate="$project_dir/bin/$CONFIGURATION/net8.0/$project_name.dhplugin"

    if [[ -f "$project_dir/$project_name.dhplugin" ]]; then
        package_path="$project_dir/$project_name.dhplugin"
    elif [[ -f "$output_candidate" ]]; then
        package_path="$output_candidate"
    else
        latest_candidate="$(find "$project_dir" -maxdepth 6 -name "*.dhplugin" -type f | head -n 1 || true)"
        if [[ -n "$latest_candidate" ]]; then
            package_path="$latest_candidate"
        fi
    fi

    if [[ -z "$package_path" || ! -f "$package_path" ]]; then
        echo "ERROR: no .dhplugin package produced for $project_name."
        exit 1
    fi

    cp "$package_path" "$PLUGIN_OUTPUT_DIR/$project_name.dhplugin"
    echo "Packed: $PLUGIN_OUTPUT_DIR/$project_name.dhplugin"
    PACKED_COUNT=$((PACKED_COUNT + 1))
done <<< "$PLUGIN_PROJECTS"

echo ""
echo "========================================"
echo "Plugin packaging completed"
echo "Total plugins: $PACKED_COUNT"
echo "Output dir: $PLUGIN_OUTPUT_DIR"
echo "========================================"
