#!/bin/bash
PACKAGERUNTIME=$1
PRECACHE=$2

NODE_URL=https://nodejs.org/dist
NODE12_VERSION="12.13.1"

get_abs_path() {
  # exploits the fact that pwd will print abs path when no args
  echo "$(cd "$(dirname "$1")" && pwd)/$(basename "$1")"
}

LAYOUT_DIR=$(get_abs_path "$(dirname $0)/../../_layout")
DOWNLOAD_DIR="$(get_abs_path "$(dirname $0)/../../_downloads")/netcore2x"

function failed() {
   local error=${1:-Undefined error}
   echo "Failed: $error" >&2
   exit 1
}

function checkRC() {
    local rc=$?
    if [ $rc -ne 0 ]; then
        failed "${1} failed with return code $rc"
    fi
}

function acquireExternalTool() {
    local download_source=$1 # E.g. https://github.com/microsoft/vswhere/releases/download/2.6.7/vswhere.exe
    local target_dir="$LAYOUT_DIR/externals/$2" # E.g. $LAYOUT_DIR/externals/vswhere
    local fix_nested_dir=$3 # Flag that indicates whether to move nested contents up one directory.

    # Extract the portion of the URL after the protocol. E.g. github.com/microsoft/vswhere/releases/download/2.6.7/vswhere.exe
    local relative_url="${download_source#*://}"

    # Check if the download already exists.
    local download_target="$DOWNLOAD_DIR/$relative_url"
    local download_basename="$(basename "$download_target")"
    local download_dir="$(dirname "$download_target")"

    if [[ "$PRECACHE" != "" ]]; then
        if [ -f "$download_target" ]; then
            echo "Download exists: $download_basename"
        else
            # Delete any previous partial file.
            local partial_target="$DOWNLOAD_DIR/partial/$download_basename"
            mkdir -p "$(dirname "$partial_target")" || checkRC 'mkdir'
            if [ -f "$partial_target" ]; then
                rm "$partial_target" || checkRC 'rm'
            fi

            # Download from source to the partial file.
            echo "Downloading $download_source"
            mkdir -p "$(dirname "$download_target")" || checkRC 'mkdir'
            # curl -f Fail silently (no output at all) on HTTP errors (H)
            #      -k Allow connections to SSL sites without certs (H)
            #      -S Show error. With -s, make curl show errors when they occur
            #      -L Follow redirects (H)
            #      -o FILE    Write to FILE instead of stdout
            curl -fkSL -o "$partial_target" "$download_source" 2>"${download_target}_download.log" || checkRC 'curl'

            # Move the partial file to the download target.
            mv "$partial_target" "$download_target" || checkRC 'mv'

            # Extract to current directory
            # Ensure we can extract those files
            # We might use them during dev.sh
            if [[ "$download_basename" == *.zip ]]; then
                # Extract the zip.
                echo "Testing zip"
                unzip "$download_target" -d "$download_dir" > /dev/null
                local rc=$?
                if [[ $rc -ne 0 && $rc -ne 1 ]]; then
                    failed "unzip failed with return code $rc"
                fi
            elif [[ "$download_basename" == *.tar.gz ]]; then
                # Extract the tar gz.
                echo "Testing tar gz"
                tar xzf "$download_target" -C "$download_dir" > /dev/null || checkRC 'tar'
            fi
        fi
    else
        # Extract to layout.
        mkdir -p "$target_dir" || checkRC 'mkdir'
        local nested_dir=""
        if [[ "$download_basename" == *.zip ]]; then
            # Extract the zip.
            echo "Extracting zip to layout"
            unzip "$download_target" -d "$target_dir" > /dev/null
            local rc=$?
            if [[ $rc -ne 0 && $rc -ne 1 ]]; then
                failed "unzip failed with return code $rc"
            fi

            # Capture the nested directory path if the fix_nested_dir flag is set.
            if [[ "$fix_nested_dir" == "fix_nested_dir" ]]; then
                nested_dir="${download_basename%.zip}" # Remove the trailing ".zip".
            fi
        elif [[ "$download_basename" == *.tar.gz ]]; then
            # Extract the tar gz.
            echo "Extracting tar gz to layout"
            tar xzf "$download_target" -C "$target_dir" > /dev/null || checkRC 'tar'

            # Capture the nested directory path if the fix_nested_dir flag is set.
            if [[ "$fix_nested_dir" == "fix_nested_dir" ]]; then
                nested_dir="${download_basename%.tar.gz}" # Remove the trailing ".tar.gz".
            fi
        else
            # Copy the file.
            echo "Copying to layout"
            cp "$download_target" "$target_dir/" || checkRC 'cp'
        fi

        # Fixup the nested directory.
        if [[ "$nested_dir" != "" ]]; then
            if [ -d "$target_dir/$nested_dir" ]; then
                mv "$target_dir/$nested_dir"/* "$target_dir/" || checkRC 'mv'
                rmdir "$target_dir/$nested_dir" || checkRC 'rmdir'
            fi
        fi
    fi
}

# Download the external tools only for Windows.
if [[ "$PACKAGERUNTIME" == "win-x64" || "$PACKAGERUNTIME" == "win-x86" ]]; then
    acquireExternalTool "$NODE_URL/v${NODE12_VERSION}/$PACKAGERUNTIME/node.exe" node12/bin
    acquireExternalTool "$NODE_URL/v${NODE12_VERSION}/$PACKAGERUNTIME/node.lib" node12/bin
    if [[ "$PRECACHE" != "" ]]; then
        acquireExternalTool "https://github.com/microsoft/vswhere/releases/download/2.6.7/vswhere.exe" vswhere
    fi
fi

# Download the external tools only for OSX.
if [[ "$PACKAGERUNTIME" == "osx-x64" ]]; then
    acquireExternalTool "$NODE_URL/v${NODE12_VERSION}/node-v${NODE12_VERSION}-darwin-x64.tar.gz" node12 fix_nested_dir
fi

# Download the external tools for Linux PACKAGERUNTIMEs.
if [[ "$PACKAGERUNTIME" == "linux-x64" ]]; then
    acquireExternalTool "$NODE_URL/v${NODE12_VERSION}/node-v${NODE12_VERSION}-linux-x64.tar.gz" node12 fix_nested_dir
    acquireExternalTool "https://vstsagenttools.blob.core.windows.net/tools/nodejs/${NODE12_VERSION}/alpine/x64/node-${NODE12_VERSION}-alpine-x64.tar.gz" node12_alpine
fi

if [[ "$PACKAGERUNTIME" == "linux-arm64" ]]; then
    acquireExternalTool "$NODE_URL/v${NODE12_VERSION}/node-v${NODE12_VERSION}-linux-arm64.tar.gz" node12 fix_nested_dir
fi

if [[ "$PACKAGERUNTIME" == "linux-arm" ]]; then
    acquireExternalTool "$NODE_URL/v${NODE12_VERSION}/node-v${NODE12_VERSION}-linux-armv7l.tar.gz" node12 fix_nested_dir
fi
