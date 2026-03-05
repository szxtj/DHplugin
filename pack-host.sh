#!/usr/bin/env bash

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR"

CONFIGURATION="${CONFIGURATION:-Release}"
VERSION="${VERSION:-$(date +%Y%m%d)}"
OUTPUT_ROOT="${OUTPUT_ROOT:-$SCRIPT_DIR/dist}"
HOST_OUTPUT_ROOT="$OUTPUT_ROOT/host"
STAGING_ROOT="$OUTPUT_ROOT/.staging-host"
CLEAN="${CLEAN:-1}"

if [[ "$CLEAN" == "1" ]]; then
    rm -rf "$HOST_OUTPUT_ROOT" "$STAGING_ROOT"
fi
mkdir -p "$HOST_OUTPUT_ROOT" "$STAGING_ROOT"

RID_LIST=("win-x64" "osx-arm64" "linux-x64" "linux-arm64")

if [[ "$CONFIGURATION" != "Release" ]]; then
    echo "ERROR: pack-host.sh requires CONFIGURATION=Release for distribution builds."
    exit 1
fi

if ! command -v zip >/dev/null 2>&1; then
    echo "ERROR: zip command not found."
    exit 1
fi

if ! command -v tar >/dev/null 2>&1; then
    echo "ERROR: tar command not found."
    exit 1
fi

rid_to_platform() {
    case "$1" in
        win-x64) echo "windows-x64" ;;
        osx-arm64) echo "macos-arm64" ;;
        linux-x64) echo "linux-x64" ;;
        linux-arm64) echo "linux-arm64" ;;
        *) return 1 ;;
    esac
}

binary_name_for_rid() {
    case "$1" in
        win-x64) echo "DHPlugin.Host.exe" ;;
        *) echo "DHPlugin.Host" ;;
    esac
}

create_run_script() {
    local rid="$1"
    local package_dir="$2"

    if [[ "$rid" == win-x64 ]]; then
        return 0
    fi

    if [[ "$rid" == osx-arm64 ]]; then
        cat > "$package_dir/run.command" << 'EOF'
#!/bin/bash
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR"
exec "$SCRIPT_DIR/DHPlugin.Host" "$@"
EOF
        chmod +x "$package_dir/run.command"
        return 0
    fi

    cat > "$package_dir/run.sh" << 'EOF'
#!/bin/bash
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR"
export DOTNET_ROOT="$SCRIPT_DIR"
exec "$SCRIPT_DIR/DHPlugin.Host" "$@"
EOF
    chmod +x "$package_dir/run.sh"
}

create_readme() {
    local rid="$1"
    local package_dir="$2"

    cat > "$package_dir/README.txt" << EOF
DHPlugin Host Package
=====================

Runtime: $rid
Version: $VERSION

This package contains only the host program.
Plugin packages are distributed separately.

EOF
}

package_windows_zip() {
    local package_name="$1"
    local package_dir_parent="$2"
    local output_dir="$3"

    mkdir -p "$output_dir"
    (
        cd "$package_dir_parent"
        zip -qry "$output_dir/$package_name.zip" "$package_name"
    )
}

package_linux_tar() {
    local package_name="$1"
    local package_dir_parent="$2"
    local output_dir="$3"

    mkdir -p "$output_dir"
    tar -czf "$output_dir/$package_name.tar.gz" -C "$package_dir_parent" "$package_name"
}

package_macos_dmg() {
    local package_name="$1"
    local package_dir_parent="$2"
    local output_dir="$3"

    if [[ "$(uname -s)" != "Darwin" ]]; then
        echo "ERROR: macOS DMG creation requires running pack-host.sh on macOS."
        exit 1
    fi

    mkdir -p "$output_dir"
    hdiutil create \
        -volname "$package_name" \
        -srcfolder "$package_dir_parent/$package_name" \
        -ov \
        -format UDZO \
        "$output_dir/$package_name.dmg" >/dev/null
}

echo "========================================"
echo "  DHPlugin Host Packaging"
echo "========================================"
echo "Configuration: $CONFIGURATION"
echo "Version: $VERSION"
echo "Output root: $HOST_OUTPUT_ROOT"
echo ""

for rid in "${RID_LIST[@]}"; do
    platform="$(rid_to_platform "$rid")"
    binary_name="$(binary_name_for_rid "$rid")"

    platform_output_dir="$HOST_OUTPUT_ROOT/$platform"
    publish_dir="$STAGING_ROOT/publish/$platform"
    package_parent="$STAGING_ROOT/package/$platform"
    package_name="DHPlugin-Host-$platform-$VERSION"
    package_dir="$package_parent/$package_name"

    echo "----------------------------------------"
    echo "Publishing host for $platform ($rid)"

    rm -rf "$publish_dir" "$package_parent"
    mkdir -p "$publish_dir" "$package_parent"

    dotnet publish src/DHPlugin.Host/DHPlugin.Host.csproj \
        -c "$CONFIGURATION" \
        -r "$rid" \
        --self-contained true \
        -p:PublishSingleFile=true \
        -p:IncludeNativeLibrariesForSelfExtract=true \
        -o "$publish_dir"

    mkdir -p "$package_dir"
    cp -R "$publish_dir/"* "$package_dir/"

    if [[ ! -f "$package_dir/$binary_name" ]]; then
        echo "ERROR: missing host binary for $platform: $binary_name"
        exit 1
    fi

    create_run_script "$rid" "$package_dir"
    create_readme "$rid" "$package_dir"

    case "$rid" in
        win-x64)
            package_windows_zip "$package_name" "$package_parent" "$platform_output_dir"
            echo "Packed: $platform_output_dir/$package_name.zip"
            ;;
        linux-x64|linux-arm64)
            package_linux_tar "$package_name" "$package_parent" "$platform_output_dir"
            echo "Packed: $platform_output_dir/$package_name.tar.gz"
            ;;
        osx-arm64)
            package_macos_dmg "$package_name" "$package_parent" "$platform_output_dir"
            echo "Packed: $platform_output_dir/$package_name.dmg"
            ;;
        *)
            echo "ERROR: unsupported rid: $rid"
            exit 1
            ;;
    esac
done

echo ""
echo "========================================"
echo "Host packaging completed"
echo "Output dir: $HOST_OUTPUT_ROOT"
echo "========================================"
