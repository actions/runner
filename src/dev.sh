#!/bin/bash

###############################################################################
#  
#  ./dev.sh build/layout/test/package [Debug/Release]
#
###############################################################################

set -e

DEV_CMD=$1
DEV_CONFIG=$2
DEV_TARGET_RUNTIME=$3

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
LAYOUT_DIR="$SCRIPT_DIR/../_layout"
DOWNLOAD_DIR="$SCRIPT_DIR/../_downloads/netcore2x"
PACKAGE_DIR="$SCRIPT_DIR/../_package"
DOTNETSDK_ROOT="$SCRIPT_DIR/../_dotnetsdk"
DOTNETSDK_VERSION="3.1.100"
DOTNETSDK_INSTALLDIR="$DOTNETSDK_ROOT/$DOTNETSDK_VERSION"
RUNNER_VERSION=$(cat runnerversion)

pushd "$SCRIPT_DIR"

BUILD_CONFIG="Debug"
if [[ "$DEV_CONFIG" == "Release" ]]; then
    BUILD_CONFIG="Release"
fi

CURRENT_PLATFORM="windows"
if [[ ($(uname) == "Linux") || ($(uname) == "Darwin") ]]; then
    CURRENT_PLATFORM=$(uname | awk '{print tolower($0)}')
fi

if [[ "$CURRENT_PLATFORM" == 'windows' ]]; then
    RUNTIME_ID='win-x64'
    if [[ "$PROCESSOR_ARCHITECTURE" == 'x86' ]]; then
        RUNTIME_ID='win-x86'
    fi
elif [[ "$CURRENT_PLATFORM" == 'linux' ]]; then
    RUNTIME_ID="linux-x64"
    if command -v uname > /dev/null; then
        CPU_NAME=$(uname -m)
        case $CPU_NAME in
            armv7l) RUNTIME_ID="linux-arm";;
            aarch64) RUNTIME_ID="linux-arm64";;
        esac
    fi
elif [[ "$CURRENT_PLATFORM" == 'darwin' ]]; then
    RUNTIME_ID='osx-x64'
fi

if [[ -n "$DEV_TARGET_RUNTIME" ]]; then
    RUNTIME_ID="$DEV_TARGET_RUNTIME"
fi

# Make sure current platform support publish the dotnet runtime
# Windows can publish win-x86/x64
# Linux can publish linux-x64/arm/arm64
# OSX can publish osx-x64
if [[ "$CURRENT_PLATFORM" == 'windows' ]]; then
    if [[ ("$RUNTIME_ID" != 'win-x86') && ("$RUNTIME_ID" != 'win-x64') ]]; then
        echo "Failed: Can't build $RUNTIME_ID package $CURRENT_PLATFORM" >&2
        exit 1
    fi
elif [[ "$CURRENT_PLATFORM" == 'linux' ]]; then
    if [[ ("$RUNTIME_ID" != 'linux-x64') && ("$RUNTIME_ID" != 'linux-x86') && ("$RUNTIME_ID" != 'linux-arm64') && ("$RUNTIME_ID" != 'linux-arm') ]]; then
       echo "Failed: Can't build $RUNTIME_ID package $CURRENT_PLATFORM" >&2
       exit 1
    fi
elif [[ "$CURRENT_PLATFORM" == 'darwin' ]]; then
    if [[ ("$RUNTIME_ID" != 'osx-x64') ]]; then
       echo "Failed: Can't build $RUNTIME_ID package $CURRENT_PLATFORM" >&2
       exit 1
    fi
fi

function failed()
{
    local error=${1:-Undefined error}
    echo "Failed: $error" >&2
    popd
    exit 1
}

function warn()
{
    local error=${1:-Undefined error}
    echo "WARNING - FAILED: $error" >&2
}

function checkRC() {
    local rc=$?
    if [ $rc -ne 0 ]; then
        failed "${1} Failed with return code $rc"
    fi
}

function heading()
{
    echo
    echo
    echo "-----------------------------------------"
    echo "  ${1}"
    echo "-----------------------------------------"
}

function build ()
{
    heading "Building ..."
    dotnet msbuild -t:Build -p:PackageRuntime="${RUNTIME_ID}" -p:BUILDCONFIG="${BUILD_CONFIG}" -p:RunnerVersion="${RUNNER_VERSION}" ./dir.proj || failed build
}

function layout ()
{
    heading "Create layout ..."
    dotnet msbuild -t:layout -p:PackageRuntime="${RUNTIME_ID}" -p:BUILDCONFIG="${BUILD_CONFIG}" -p:RunnerVersion="${RUNNER_VERSION}" ./dir.proj || failed build

    #change execution flag to allow running with sudo
    if [[ ("$CURRENT_PLATFORM" == "linux") || ("$CURRENT_PLATFORM" == "darwin") ]]; then
        chmod +x "${LAYOUT_DIR}/bin/Runner.Listener"
        chmod +x "${LAYOUT_DIR}/bin/Runner.Worker"
        chmod +x "${LAYOUT_DIR}/bin/Runner.PluginHost"
        chmod +x "${LAYOUT_DIR}/bin/installdependencies.sh"
    fi

    heading "Setup externals folder for $RUNTIME_ID runner's layout"
    bash ./Misc/externals.sh $RUNTIME_ID || checkRC externals.sh
}

function runtest ()
{
    heading "Testing ..."

    if [[ ("$CURRENT_PLATFORM" == "linux") || ("$CURRENT_PLATFORM" == "darwin") ]]; then
        ulimit -n 1024
    fi

    dotnet msbuild -t:test -p:PackageRuntime="${RUNTIME_ID}" -p:BUILDCONFIG="${BUILD_CONFIG}" -p:RunnerVersion="${RUNNER_VERSION}" ./dir.proj || failed "failed tests" 
}

function package ()
{
    if [ ! -d "${LAYOUT_DIR}/bin" ]; then
        echo "You must build first.  Expecting to find ${LAYOUT_DIR}/bin"
    fi

    # TODO: We are cross-compiling arm on x64 so we cant exec Runner.Listener. Remove after building on native arm host
    runner_ver=$("${LAYOUT_DIR}/bin/Runner.Listener" --version) || runner_ver=$(cat runnerversion) || failed "version"
    runner_pkg_name="actions-runner-${RUNTIME_ID}-${runner_ver}"

    heading "Packaging ${runner_pkg_name}"

    rm -Rf "${LAYOUT_DIR:?}/_diag"
    find "${LAYOUT_DIR}/bin" -type f -name '*.pdb' -delete

    mkdir -p "$PACKAGE_DIR"
    rm -Rf "${PACKAGE_DIR:?}"/*

    pushd "$PACKAGE_DIR" > /dev/null

    if [[ ("$CURRENT_PLATFORM" == "linux") || ("$CURRENT_PLATFORM" == "darwin") ]]; then
        tar_name="${runner_pkg_name}.tar.gz"
        echo "Creating $tar_name in ${LAYOUT_DIR}"
        tar -czf "${tar_name}" -C "${LAYOUT_DIR}" .
    elif [[ ("$CURRENT_PLATFORM" == "windows") ]]; then
        zip_name="${runner_pkg_name}.zip"
        echo "Convert ${LAYOUT_DIR} to Windows style path"
        window_path=${LAYOUT_DIR:1}
        window_path=${window_path:0:1}:${window_path:1}
        echo "Creating $zip_name in ${window_path}"
        powershell -NoLogo -Sta -NoProfile -NonInteractive -ExecutionPolicy Unrestricted -Command "Add-Type -Assembly \"System.IO.Compression.FileSystem\"; [System.IO.Compression.ZipFile]::CreateFromDirectory(\"${window_path}\", \"${zip_name}\")"
    fi

    runner_patch_pkg_name="actions-runner-${RUNTIME_ID}-${runner_ver}-patch"

    heading "Packaging Patch Version ${runner_patch_pkg_name}"

    echo "Downloading latest runner ..."

    mkdir -p "_temp"
    pushd "_temp" > /dev/null

    latest_version_label=$(curl -s -X GET 'https://api.github.com/repos/actions/runner/releases/latest' | jq -r '.tag_name')
    latest_version=$(echo ${latest_version_label:1})
    latest_version_runner_file=""
    if [[ ("$CURRENT_PLATFORM" == "linux") || ("$CURRENT_PLATFORM" == "darwin") ]]; then
        latest_version_runner_file="actions-runner-${RUNTIME_ID}-${latest_version}.tar.gz"
    elif [[ ("$CURRENT_PLATFORM" == "windows") ]]; then
        latest_version_runner_file="actions-runner-${RUNTIME_ID}-${latest_version}.zip"
    fi

    latest_version_download_url="https://github.com/actions/runner/releases/download/${latest_version_label}/${latest_version_runner_file}"

    echo "Downloading ${latest_version_label} for ${RUNTIME_ID} ..."
    echo $latest_version_download_url

    curl -O -L ${latest_version_download_url}

    mkdir -p "latest_runner"
    if [[ ("$CURRENT_PLATFORM" == "linux") || ("$CURRENT_PLATFORM" == "darwin") ]]; then
        tar xzf "./${latest_version_runner_file}" -C latest_runner
    elif [[ ("$CURRENT_PLATFORM" == "windows") ]]; then
        powershell -NoLogo -Sta -NoProfile -NonInteractive -ExecutionPolicy Unrestricted -Command "Add-Type -Assembly \"System.IO.Compression.FileSystem\"; [System.IO.Compression.ZipFile]::ExtractToDirectory(\"./${latest_version_runner_file}\", \"latest_runner\")"
    fi

    mkdir -p "_patch"
    diff -qrN "${LAYOUT_DIR}" "latest_runner" | cut -d " " -f 2 | xargs -t -I file cp file "_patch/"$(echo file | cut -c ${#LAYOUT_DIR}- | cut -c 3-)

    popd > /dev/null

    if [[ ("$CURRENT_PLATFORM" == "linux") || ("$CURRENT_PLATFORM" == "darwin") ]]; then
        patch_tar_name="${runner_patch_pkg_name}.tar.gz"
        echo "Creating $patch_tar_name in ${PACKAGE_DIR}/_temp/_patch"
        tar -czf "${patch_tar_name}" -C "${PACKAGE_DIR}/_temp/_patch" .
    elif [[ ("$CURRENT_PLATFORM" == "windows") ]]; then
        patch_zip_name="${runner_patch_pkg_name}.zip"
        echo "Convert ${PACKAGE_DIR} to Windows style path"
        window_path=${PACKAGE_DIR:1}
        window_path=${window_path:0:1}:${window_path:1}
        echo "Creating $patch_zip_name in ${window_path}/_temp/_patch"
        powershell -NoLogo -Sta -NoProfile -NonInteractive -ExecutionPolicy Unrestricted -Command "Add-Type -Assembly \"System.IO.Compression.FileSystem\"; [System.IO.Compression.ZipFile]::CreateFromDirectory(\"${window_path}\", \"${patch_zip_name}\")"
    fi
    
    rm -Rf "${PACKAGE_DIR}/_temp"
    popd > /dev/null
}

if [[ (! -d "${DOTNETSDK_INSTALLDIR}") || (! -e "${DOTNETSDK_INSTALLDIR}/.${DOTNETSDK_VERSION}") || (! -e "${DOTNETSDK_INSTALLDIR}/dotnet") ]]; then
    
    # Download dotnet SDK to ../_dotnetsdk directory
    heading "Ensure Dotnet SDK"

    # _dotnetsdk
    #           \1.0.x
    #                            \dotnet
    #                            \.1.0.x
    echo "Download dotnetsdk into ${DOTNETSDK_INSTALLDIR}"
    rm -Rf "${DOTNETSDK_DIR}"

    # run dotnet-install.ps1 on windows, dotnet-install.sh on linux
    if [[ ("$CURRENT_PLATFORM" == "windows") ]]; then
        echo "Convert ${DOTNETSDK_INSTALLDIR} to Windows style path"
        sdkinstallwindow_path=${DOTNETSDK_INSTALLDIR:1}
        sdkinstallwindow_path=${sdkinstallwindow_path:0:1}:${sdkinstallwindow_path:1}
        powershell -NoLogo -Sta -NoProfile -NonInteractive -ExecutionPolicy Unrestricted -Command "& \"./Misc/dotnet-install.ps1\" -Version ${DOTNETSDK_VERSION} -InstallDir \"${sdkinstallwindow_path}\" -NoPath; exit \$LastExitCode;" || checkRC dotnet-install.ps1
    else
        bash ./Misc/dotnet-install.sh --version ${DOTNETSDK_VERSION} --install-dir "${DOTNETSDK_INSTALLDIR}" --no-path || checkRC dotnet-install.sh
    fi

    echo "${DOTNETSDK_VERSION}" > "${DOTNETSDK_INSTALLDIR}/.${DOTNETSDK_VERSION}"
fi

echo "Prepend ${DOTNETSDK_INSTALLDIR} to %PATH%"
export PATH=${DOTNETSDK_INSTALLDIR}:$PATH

heading "Dotnet SDK Version"
dotnet --version

heading "Pre-cache external resources for $RUNTIME_ID package ..."
bash ./Misc/externals.sh $RUNTIME_ID "Pre-Cache" || checkRC "externals.sh Pre-Cache"

if [[ "$CURRENT_PLATFORM" == 'windows' ]]; then
    vswhere=$(find "$DOWNLOAD_DIR" -name vswhere.exe | head -1)
    vs_location=$("$vswhere" -prerelease -latest -property installationPath)
    msbuild_location="$vs_location""\MSBuild\15.0\Bin\msbuild.exe"

    if [[ ! -e "${msbuild_location}" ]]; then
        msbuild_location="$vs_location""\MSBuild\Current\Bin\msbuild.exe"

        if [[ ! -e "${msbuild_location}" ]]; then
            failed "Can not find msbuild location, failing build"
        fi
    fi

    export DesktopMSBuild="$msbuild_location"
fi

case $DEV_CMD in
    "build") build;;
    "b") build;;
    "test") runtest;;
    "t") runtest;;
    "layout") layout;;
    "l") layout;;
    "package") package;;
    "p") package;;
    *) echo "Invalid cmd.  Use build(b), test(t), layout(l) or package(p)";;
esac

popd
echo
echo Done.
echo
