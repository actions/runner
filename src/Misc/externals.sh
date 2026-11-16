#!/bin/bash
PACKAGERUNTIME=$1
PRECACHE=$2

NODE_URL=https://nodejs.org/dist
NODE_ALPINE_URL=https://github.com/actions/alpine_nodejs/releases/download
# When you update Node versions you must also create a new release of alpine_nodejs at that updated version.
# Follow the instructions here: https://github.com/actions/alpine_nodejs?tab=readme-ov-file#getting-started
NODE24_VERSION="24.22.3"
NPM_VERSION="11.21.2"
NPM_URL=https://registry.npmjs.org/npm/-/npm-${NPM_VERSION}.tgz
NPM_INTEGRITY="sha512-7HPApk+AflMUavSseL1Pm6CnYqMIHk53x+KmuhqmTDqPkjPMgxIpTPcTqsE91EKNw4gFBJV9fnXiSdGm1DXq9A=="
NPM_UNDICI_VERSION="6.29.1"

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

            CURL_VERSION=$(curl --version | awk 'NR==1{print $2}')
            echo "Curl version: $CURL_VERSION"

            # curl -f Fail silently (no output at all) on HTTP errors (H)
            #      -S Show error. With -s, make curl show errors when they occur
            #      -L Follow redirects (H)
            #      -o FILE    Write to FILE instead of stdout
            #      --retry 3   Retries transient errors 3 times (timeouts, 5xx)
            if [[ "$(printf '%s\n' "7.71.0" "$CURL_VERSION" | sort -V | head -n1)" != "7.71.0" ]]; then
                # Curl version is less than or equal to 7.71.0, skipping retry-all-errors flag
                 curl -fSL --retry 3 -o "$partial_target" "$download_source" 2>"${download_target}_download.log" || checkRC 'curl'
            else
                # Curl version is greater than 7.71.0, running curl with --retry-all-errors flag
                 curl -fSL --retry 3 --retry-all-errors -o "$partial_target" "$download_source" 2>"${download_target}_download.log" || checkRC 'curl'
            fi

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

function acquireNpmPackage() {
    local relative_url="${NPM_URL#*://}"
    local download_target="$DOWNLOAD_DIR/$relative_url"
    local download_basename="$(basename "$download_target")"
    local download_dir="$(dirname "$download_target")"

    if [ ! -f "$download_target" ]; then
        local partial_target="$DOWNLOAD_DIR/partial/$download_basename"
        mkdir -p "$(dirname "$partial_target")" || checkRC 'mkdir'
        rm -f "$partial_target" || checkRC 'rm'

        echo "Downloading $NPM_URL" >&2
        mkdir -p "$download_dir" || checkRC 'mkdir'

        CURL_VERSION=$(curl --version | awk 'NR==1{print $2}')
        echo "Curl version: $CURL_VERSION" >&2

        if [[ "$(printf '%s\n' "7.71.0" "$CURL_VERSION" | sort -V | head -n1)" != "7.71.0" ]]; then
            curl -fSL --retry 3 -o "$partial_target" "$NPM_URL" 2>"${download_target}_download.log" || checkRC 'curl'
        else
            curl -fSL --retry 3 --retry-all-errors -o "$partial_target" "$NPM_URL" 2>"${download_target}_download.log" || checkRC 'curl'
        fi

        mv "$partial_target" "$download_target" || checkRC 'mv'
    fi

    echo "$download_target"
}

function updateNpmPackage() {
    local target_dir="$1"
    local node="$target_dir/bin/node"
    local npm_cli="$target_dir/lib/node_modules/npm/bin/npm-cli.js"
    local npm_package="$2"

    [ -x "$node" ] || failed "node executable is missing from $target_dir"
    [ -f "$npm_cli" ] || failed "npm CLI is missing from $target_dir"

    local actual_integrity=$("$node" -e "const crypto = require('crypto'); const fs = require('fs'); process.stdout.write('sha512-' + crypto.createHash('sha512').update(fs.readFileSync(process.argv[1])).digest('base64'))" "$npm_package")
    [ "$actual_integrity" = "$NPM_INTEGRITY" ] || failed "npm package integrity check failed"

    "$node" "$npm_cli" install --global --prefix "$target_dir" --ignore-scripts --no-audit --no-fund "$npm_package" || checkRC 'npm install'

    local undici_version=$("$node" -p "require('$target_dir/lib/node_modules/npm/node_modules/undici/package.json').version")
    [ "$undici_version" = "$NPM_UNDICI_VERSION" ] || failed "npm bundled undici version is $undici_version, expected $NPM_UNDICI_VERSION"
}

# Download the external tools only for Windows.
if [[ "$PACKAGERUNTIME" == "win-x64" || "$PACKAGERUNTIME" == "win-x86" ]]; then
    acquireExternalTool "$NODE_URL/v${NODE24_VERSION}/$PACKAGERUNTIME/node.exe" node24/bin
    acquireExternalTool "$NODE_URL/v${NODE24_VERSION}/$PACKAGERUNTIME/node.lib" node24/bin
    if [[ "$PRECACHE" != "" ]]; then
        acquireExternalTool "https://github.com/microsoft/vswhere/releases/download/2.6.7/vswhere.exe" vswhere
    fi
fi

# Download the external tools only for Windows.
if [[ "$PACKAGERUNTIME" == "win-arm64" ]]; then
    # todo: replace these with official release when available
    acquireExternalTool "$NODE_URL/v${NODE24_VERSION}/$PACKAGERUNTIME/node.exe" node24/bin
    acquireExternalTool "$NODE_URL/v${NODE24_VERSION}/$PACKAGERUNTIME/node.lib" node24/bin
    if [[ "$PRECACHE" != "" ]]; then
        acquireExternalTool "https://github.com/microsoft/vswhere/releases/download/2.6.7/vswhere.exe" vswhere
    fi
fi

# Download the external tools only for OSX.
if [[ "$PACKAGERUNTIME" == "osx-x64" ]]; then
    acquireExternalTool "$NODE_URL/v${NODE24_VERSION}/node-v${NODE24_VERSION}-darwin-x64.tar.gz" node24 fix_nested_dir
fi

if [[ "$PACKAGERUNTIME" == "osx-arm64" ]]; then
    acquireExternalTool "$NODE_URL/v${NODE24_VERSION}/node-v${NODE24_VERSION}-darwin-arm64.tar.gz" node24 fix_nested_dir
fi

# Download the external tools for Linux PACKAGERUNTIMEs.
if [[ "$PACKAGERUNTIME" == "linux-x64" ]]; then
    acquireExternalTool "$NODE_URL/v${NODE24_VERSION}/node-v${NODE24_VERSION}-linux-x64.tar.gz" node24 fix_nested_dir
    acquireExternalTool "$NODE_ALPINE_URL/v${NODE24_VERSION}/node-v${NODE24_VERSION}-alpine-x64.tar.gz" node24_alpine
fi

if [[ "$PACKAGERUNTIME" == "linux-arm64" ]]; then
    acquireExternalTool "$NODE_URL/v${NODE24_VERSION}/node-v${NODE24_VERSION}-linux-arm64.tar.gz" node24 fix_nested_dir
fi

if [[ "$PACKAGERUNTIME" == "osx-x64" || "$PACKAGERUNTIME" == "osx-arm64" || "$PACKAGERUNTIME" == "linux-x64" || "$PACKAGERUNTIME" == "linux-arm64" ]]; then
    npm_package=$(acquireNpmPackage)
    if [[ "$PRECACHE" == "" ]]; then
        updateNpmPackage "$LAYOUT_DIR/externals/node24" "$npm_package"
        if [[ "$PACKAGERUNTIME" == "linux-x64" ]]; then
            updateNpmPackage "$LAYOUT_DIR/externals/node24_alpine" "$npm_package"
        fi
    fi
fi
