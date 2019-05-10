#!/bin/bash
PACKAGERUNTIME=$1
PRECACHE=$2

CONTAINER_URL=https://vstsagenttools.blob.core.windows.net/tools
NODE_URL=https://nodejs.org/dist
NODE_VERSION="6.10.3"
NODE10_VERSION="10.13.0"
PRODUCT="Github"

get_abs_path() {
  # exploits the fact that pwd will print abs path when no args
  echo "$(cd "$(dirname "$1")" && pwd)/$(basename "$1")"
}

LAYOUT_DIR=$(get_abs_path "$(dirname $0)/../../_layout")
DOWNLOAD_DIR="$(get_abs_path "$(dirname $0)/../../_downloads")/netcore2x"

if [[ "$AZURE_PIPELINES_AGENT" != "" ]]; then
    PRODUCT="AzurePipelines"
fi

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
    local download_source=$1 # E.g. https://vstsagenttools.blob.core.windows.net/tools/pdbstr/1/pdbstr.zip
    local target_dir="$LAYOUT_DIR/externals/$2" # E.g. $LAYOUT_DIR/externals/pdbstr
    local fix_nested_dir=$3 # Flag that indicates whether to move nested contents up one directory. E.g. TEE-CLC-14.0.4.zip
                            # directly contains only a nested directory TEE-CLC-14.0.4. When this flag is set, the contents
                            # of the nested TEE-CLC-14.0.4 directory are moved up one directory, and then the empty directory
                            # TEE-CLC-14.0.4 is removed.

    # Extract the portion of the URL after the protocol. E.g. vstsagenttools.blob.core.windows.net/tools/pdbstr/1/pdbstr.zip
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
if [[ "$PACKAGERUNTIME" == "win-x64" ]]; then
    if [[ "$PRODUCT" == "Github" ]]; then
        acquireExternalTool "$NODE_URL/v${NODE10_VERSION}/win-x64/node.exe" node10/bin
        acquireExternalTool "$NODE_URL/v${NODE10_VERSION}/win-x64/node.lib" node10/bin
        if [[ "$PRECACHE" != "" ]]; then
            acquireExternalTool "$CONTAINER_URL/vswhere/1_0_62/vswhere.zip" vswhere
        fi
    else
        acquireExternalTool "$CONTAINER_URL/azcopy/1/azcopy.zip" azcopy
        acquireExternalTool "$CONTAINER_URL/pdbstr/1/pdbstr.zip" pdbstr
        acquireExternalTool "$CONTAINER_URL/mingit/2.21.0/MinGit-2.21.0-64-bit.zip" git
        acquireExternalTool "$CONTAINER_URL/symstore/1/symstore.zip" symstore
        acquireExternalTool "$CONTAINER_URL/vstshost/m122_887c6659/vstshost.zip" vstshost
        acquireExternalTool "$CONTAINER_URL/vstsom/m122_887c6659/vstsom.zip" vstsom
        acquireExternalTool "$CONTAINER_URL/vswhere/1_0_62/vswhere.zip" vswhere
        acquireExternalTool "$NODE_URL/v${NODE_VERSION}/win-x64/node.exe" node/bin
        acquireExternalTool "$NODE_URL/v${NODE_VERSION}/win-x64/node.lib" node/bin
        acquireExternalTool "$NODE_URL/v${NODE10_VERSION}/win-x64/node.exe" node10/bin
        acquireExternalTool "$NODE_URL/v${NODE10_VERSION}/win-x64/node.lib" node10/bin
        acquireExternalTool "https://dist.nuget.org/win-x86-commandline/v3.3.0/nuget.exe" nuget
    fi
fi

if [[ "$PACKAGERUNTIME" == "win-x86" ]]; then
    acquireExternalTool "$CONTAINER_URL/pdbstr/1/pdbstr.zip" pdbstr
    acquireExternalTool "$CONTAINER_URL/mingit/2.21.0/MinGit-2.21.0-32-bit.zip" git
    acquireExternalTool "$CONTAINER_URL/symstore/1/symstore.zip" symstore
    acquireExternalTool "$CONTAINER_URL/vstsom/m122_887c6659/vstsom.zip" vstsom
    acquireExternalTool "$CONTAINER_URL/vswhere/1_0_62/vswhere.zip" vswhere
    acquireExternalTool "$NODE_URL/v${NODE_VERSION}/win-x86/node.exe" node/bin
    acquireExternalTool "$NODE_URL/v${NODE_VERSION}/win-x86/node.lib" node/bin
    acquireExternalTool "$NODE_URL/v${NODE10_VERSION}/win-x86/node.exe" node10/bin
    acquireExternalTool "$NODE_URL/v${NODE10_VERSION}/win-x86/node.lib" node10/bin
    acquireExternalTool "https://dist.nuget.org/win-x86-commandline/v3.3.0/nuget.exe" nuget
fi

# Download the external tools only for OSX.
if [[ "$PACKAGERUNTIME" == "osx-x64" ]]; then
    if [[ "$PRODUCT" == "Github" ]]; then
        acquireExternalTool "$NODE_URL/v${NODE10_VERSION}/node-v${NODE10_VERSION}-darwin-x64.tar.gz" node10 fix_nested_dir        
    else
        acquireExternalTool "$NODE_URL/v${NODE_VERSION}/node-v${NODE_VERSION}-darwin-x64.tar.gz" node fix_nested_dir
        acquireExternalTool "$NODE_URL/v${NODE10_VERSION}/node-v${NODE10_VERSION}-darwin-x64.tar.gz" node10 fix_nested_dir
    fi
fi

# Download the external tools common across OSX and Linux PACKAGERUNTIMEs.
if [[ "$PACKAGERUNTIME" == "linux-x64" || "$PACKAGERUNTIME" == "linux-arm" || "$PACKAGERUNTIME" == "osx-x64" || "$PACKAGERUNTIME" == "rhel.6-x64" ]]; then
    if [[ "$PRODUCT" != "Github" ]]; then
        acquireExternalTool "$CONTAINER_URL/tee/14_134_0/TEE-CLC-14.134.0.zip" tee fix_nested_dir
        acquireExternalTool "$CONTAINER_URL/vso-task-lib/0.5.5/vso-task-lib.tar.gz" vso-task-lib
    fi
fi

# Download the external tools common across Linux PACKAGERUNTIMEs (excluding OSX).
if [[ "$PACKAGERUNTIME" == "linux-x64" || "$PACKAGERUNTIME" == "rhel.6-x64" ]]; then
    if [[ "$PRODUCT" == "Github" ]]; then
        acquireExternalTool "$NODE_URL/v${NODE10_VERSION}/node-v${NODE10_VERSION}-linux-x64.tar.gz" node10 fix_nested_dir
    else
        acquireExternalTool "$NODE_URL/v${NODE_VERSION}/node-v${NODE_VERSION}-linux-x64.tar.gz" node fix_nested_dir
        acquireExternalTool "$NODE_URL/v${NODE10_VERSION}/node-v${NODE10_VERSION}-linux-x64.tar.gz" node10 fix_nested_dir
    fi
fi

if [[ "$PACKAGERUNTIME" == "linux-arm" ]]; then
    if [[ "$PRODUCT" == "Github" ]]; then
        acquireExternalTool "$NODE_URL/v${NODE10_VERSION}/node-v${NODE10_VERSION}-linux-armv7l.tar.gz" node10 fix_nested_dir	
    else
        acquireExternalTool "$NODE_URL/v${NODE_VERSION}/node-v${NODE_VERSION}-linux-armv7l.tar.gz" node fix_nested_dir
        acquireExternalTool "$NODE_URL/v${NODE10_VERSION}/node-v${NODE10_VERSION}-linux-armv7l.tar.gz" node10 fix_nested_dir	
    fi
fi
