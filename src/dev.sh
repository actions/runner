#!/bin/bash
DEV_CMD=$1
DEV_SUBCMD=$2
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
LAYOUT_DIR="$SCRIPT_DIR/../_layout"
DOWNLOAD_DIR="$SCRIPT_DIR/../_downloads"
DOTNETSDK_ROOT="$SCRIPT_DIR/../_dotnetsdk"
DOTNETSDK_VERSION="1.0.0-rc4-004771"
DOTNETSDK_INSTALLDIR="$DOTNETSDK_ROOT/$DOTNETSDK_VERSION"

pushd $SCRIPT_DIR

BUILD_CONFIG="Debug"
if [[ "$DEV_SUBCMD" == "Release" ]]; then
    BUILD_CONFIG="Release"
fi

PLATFORM_NAME=`uname`
PLATFORM="windows"
if [[ ("$PLATFORM_NAME" == "Linux") || ("$PLATFORM_NAME" == "Darwin") ]]; then
   PLATFORM=`echo "${PLATFORM_NAME}" | awk '{print tolower($0)}'`
fi

# allow for #if defs in code
define_os='OS_WINDOWS'
runtime_id='win7-x64'
if [[ "$PLATFORM" == 'linux' ]]; then
   define_os='OS_LINUX'
   if [ -e /etc/os-release ]; then
        . /etc/os-release
        case "$ID.$VERSION_ID" in
            "centos.7")
                runtime_id='centos.7-x64';;
            "rhel.7.2")
                runtime_id='rhel.7.2-x64';;
            "ubuntu.14.04")
                runtime_id='ubuntu.14.04-x64';;
            "ubuntu.16.04")
                runtime_id='ubuntu.16.04-x64';;
        esac
        if [[ ("$runtime_id" == "win7-x64") ]]; then
            failed "Can not determine runtime identifier from '$ID.$VERSION_ID'"
        fi
    else
        failed "Can not read os information from /etc/os-release"
    fi
elif [[ "$PLATFORM" == 'darwin' ]]; then
   define_os='OS_OSX'
   runtime_id='osx.10.11-x64'
fi

build_dirs=("Microsoft.VisualStudio.Services.Agent" "Agent.Listener" "Agent.Worker" "Test")
build_clean_dirs=("Agent.Listener" "Test" "Agent.Worker" "Microsoft.VisualStudio.Services.Agent")
bin_layout_dirs=("Agent.Listener" "Microsoft.VisualStudio.Services.Agent" "Agent.Worker")
WINDOWSAGENTSERVICE_PROJFILE="Agent.Service/Windows/AgentService.csproj"
WINDOWSAGENTSERVICE_BIN="Agent.Service/Windows/bin/Debug"

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
    echo -----------------------------------------
    echo   ${1}
    echo -----------------------------------------
}

function rundotnet ()
{
    dotnet_cmd=${1}
    err_handle=${2:-failed}

    if [[ ( "${!3}" == "" ) ]]; then
        run_dirs=("${3}")
    else 
        run_dirs=("${!3}")
    fi

    heading ${1} ...

    cfg_args=""
    msbuild_args=""
    runtime_args=""
    if [[ ("$dotnet_cmd" == "build") ]]; then
        cfg_args="-c ${BUILD_CONFIG}"
        if [[ "$define_os" == 'OS_WINDOWS' ]]; then
            msbuild_args="//p:OSConstant=${define_os}"
        else
            msbuild_args="/p:OSConstant=${define_os}"
        fi    
    fi

    if [[ ("$dotnet_cmd" == "publish") ]]; then
        cfg_args="-c ${BUILD_CONFIG}"
        runtime_args="--runtime ${runtime_id}"        
        if [[ "$define_os" == 'OS_WINDOWS' ]]; then
            msbuild_args="//p:OSConstant=${define_os}"
        else
            msbuild_args="/p:OSConstant=${define_os}"
        fi    
    fi

    echo "${cfg_args}"
    echo "${msbuild_args}"
    echo "${runtime_args}"

    for dir_name in ${run_dirs[@]}
    do
        echo
        echo -- Running: dotnet $dotnet_cmd $dir_name --
        echo
        dotnet ${dotnet_cmd} $runtime_args $dir_name $cfg_args $msbuild_args|| ${err_handle} "${dotnet_cmd} $dir_name"
    done   
}

function generateConstant()
{
    commit_token="_COMMIT_HASH_"
    package_token="_PACKAGE_NAME_"
    commit_hash=`git rev-parse HEAD` || failed "git commit hash"
    echo "Building ${commit_hash} --- ${runtime_id}"
    sed -e "s/$commit_token/$commit_hash/g" -e "s/$package_token/$runtime_id/g" "Misc/BuildConstants.ch" > "Microsoft.VisualStudio.Services.Agent/BuildConstants.cs"
}

function build ()
{
    generateConstant
    
    if [[ "$define_os" == 'OS_WINDOWS' ]]; then
        reg_out=`reg query "HKLM\SOFTWARE\Microsoft\MSBuild\ToolsVersions\4.0" -v MSBuildToolsPath`
        msbuild_location=`echo $reg_out | tr -d '\r\n' | tr -s ' ' | cut -d' ' -f5 | tr -d '\r\n'`
              
        local rc=$?
        if [ $rc -ne 0 ]; then
            failed "Can not find msbuild location, failing build"
        fi
    fi

    rundotnet build failed build_dirs[@]

    if [[ "$define_os" == 'OS_WINDOWS' && "$msbuild_location" != "" ]]; then
        $msbuild_location/msbuild.exe $WINDOWSAGENTSERVICE_PROJFILE
    fi
}

function restore ()
{
    rundotnet restore warn build_dirs[@]
}

function clean ()
{
    heading Cleaning ...
    for dir_name in ${build_clean_dirs[@]}
    do
        echo Cleaning ${dir_name} ...
        rm -rf `dirname ${0}`/${dir_name}/bin
        rm -rf `dirname ${0}`/${dir_name}/obj
    done
}

function publish ()
{
    rundotnet publish failed bin_layout_dirs[@]
}

function copyBin ()
{
    echo Copying ${1}
    pushd ${1}/bin/${BUILD_CONFIG}/netcoreapp1.1 > /dev/null

    source_dir=$(ls -d */)publish/
    if [ ! -d "$source_dir" ]; then
        failed "Publish folder is missing. Please ensure you use the correct .NET Core tools (see readme for instructions)"
    fi

    cp -Rf ${source_dir}* ${LAYOUT_DIR}/bin
    popd > /dev/null 
}

function layout ()
{
    clean
    restore
    build
    publish
    
    heading Layout ...
    rm -Rf ${LAYOUT_DIR}
    mkdir -p ${LAYOUT_DIR}/bin
    for bin_copy_dir in ${bin_layout_dirs[@]}
    do
        copyBin ${bin_copy_dir}
    done

    if [[ "$define_os" == 'OS_WINDOWS' ]]; then
        # TODO Make sure to package Release build instead of debug build
        echo Copying Agent.Service
        cp -Rf $WINDOWSAGENTSERVICE_BIN/* ${LAYOUT_DIR}/bin
    fi
    
    cp -Rf ./Misc/layoutroot/* ${LAYOUT_DIR}
    cp -Rf ./Misc/layoutbin/* ${LAYOUT_DIR}/bin
    
    #change execution flag to allow running with sudo
    if [[ "$PLATFORM" == 'linux' ]]; then
        chmod +x ${LAYOUT_DIR}/bin/Agent.Listener
        chmod +x ${LAYOUT_DIR}/bin/Agent.Worker
    fi

    # clean up files not meant for platform
    if [[ ("$PLATFORM_NAME" == "Linux") || ("$PLATFORM_NAME" == "Darwin") ]]; then
        rm ${LAYOUT_DIR}/*.cmd
    else
        rm ${LAYOUT_DIR}/*.sh
    fi
    
    heading Externals ...
    bash ./Misc/externals.sh || checkRC externals.sh
}

function update ()
{
    if [[ "$DEV_SUBCMD" != '' ]]; then
        update_dirs=(${DEV_SUBCMD})
    else
        update_dirs=${bin_layout_dirs[@]}
    fi

    for update_dir in ${update_dirs[@]}
    do
        echo Updating ${update_dir}
        rundotnet build failed ${update_dir}
        echo Publishing ${update_dir}
        rundotnet publish failed ${update_dir}
        copyBin ${update_dir}
    done
}

function runtest ()
{
    if [[ ("$PLATFORM" == "linux") || ("$PLATFORM" == "darwin") ]]; then
        ulimit -n 1024
    fi

    heading Testing ...
    pushd Test> /dev/null    
    export VSTS_AGENT_SRC_DIR=${SCRIPT_DIR}
    dotnet test --no-build --logger:trx || failed "failed tests"
    popd > /dev/null    
}

function validate ()
{
    echo git clean ...
    git clean -fdx || failed "git clean"

    layout
    runtest
}

function buildtest ()
{
    build
    runtest
}

function package ()
{
    # get the runtime we are build for
    # if exist Agent.Listener/bin/${BUILD_CONFIG}/netcoreapp1.1
    build_folder="Agent.Listener/bin/${BUILD_CONFIG}/netcoreapp1.1"
    if [ ! -d "${build_folder}" ]; then
        echo "You must build first.  Expecting to find ${build_folder}"
    fi

    pushd "${build_folder}" > /dev/null
    pwd
    runtime_folder=`ls -d */`

    pkg_runtime=${runtime_folder%/}
    popd > /dev/null

    pkg_dir=`pwd`/../_package

    agent_ver=`${LAYOUT_DIR}/bin/Agent.Listener --version` || failed "version"
    agent_pkg_name="vsts-agent-${pkg_runtime}-${agent_ver}"
    # -$(date +%m)$(date +%d)"

    heading "Packaging ${agent_pkg_name}"

    rm -Rf ${LAYOUT_DIR}/_diag
    find ${LAYOUT_DIR}/bin -type f -name '*.pdb' -delete
    mkdir -p $pkg_dir
    pushd $pkg_dir > /dev/null
    rm -Rf *

    if [[ ("$PLATFORM" == "linux") || ("$PLATFORM" == "darwin") ]]; then
        tar_name="${agent_pkg_name}.tar.gz"
        echo "Creating $tar_name in ${LAYOUT_DIR}"
        tar -czf "${tar_name}" -C ${LAYOUT_DIR} .
    elif [[ ("$PLATFORM" == "windows") ]]; then
        zip_name="${agent_pkg_name}.zip"
        echo "Convert ${LAYOUT_DIR} to Windows style path"
        window_path=${LAYOUT_DIR:1}
        window_path=${window_path:0:1}:${window_path:1}
        echo "Creating $zip_name in ${window_path}"
        powershell -NoLogo -Sta -NoProfile -NonInteractive -ExecutionPolicy Unrestricted -Command "Add-Type -Assembly \"System.IO.Compression.FileSystem\"; [System.IO.Compression.ZipFile]::CreateFromDirectory(\"${window_path}\", \"${zip_name}\")"
    fi

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
    rm -Rf ${DOTNETSDK_DIR}

    # run dotnet-install.ps1 on windows, dotnet-install.sh on linux
    if [[ ("$PLATFORM" == "windows") ]]; then
        echo "Convert ${DOTNETSDK_INSTALLDIR} to Windows style path"
        sdkinstallwindow_path=${DOTNETSDK_INSTALLDIR:1}
        sdkinstallwindow_path=${sdkinstallwindow_path:0:1}:${sdkinstallwindow_path:1}
        powershell -NoLogo -Sta -NoProfile -NonInteractive -ExecutionPolicy Unrestricted -Command "& \"./Misc/dotnet-install.ps1\" -Version ${DOTNETSDK_VERSION} -InstallDir \"${sdkinstallwindow_path}\" -NoPath; exit $LastExitCode;" || checkRC dotnet-install.ps1
    else
        bash ./Misc/dotnet-install.sh --version ${DOTNETSDK_VERSION} --install-dir ${DOTNETSDK_INSTALLDIR} --no-path || checkRC dotnet-install.sh
    fi

    echo "${DOTNETSDK_VERSION}" > ${DOTNETSDK_INSTALLDIR}/.${DOTNETSDK_VERSION}
fi

echo "Prepend ${DOTNETSDK_INSTALLDIR} to %PATH%"
export PATH=${DOTNETSDK_INSTALLDIR}:$PATH

heading "Dotnet SDK Version"
dotnet --version

case $DEV_CMD in
   "build") build;;
   "b") build;;
   "test") runtest;;
   "t") runtest;;
   "bt") buildtest;;   
   "clean") clean;;
   "c") clean;;
   "restore") restore;;
   "r") restore;;
   "layout") layout;;
   "l") layout;;
   "update") update;;
   "u") update;;
   "package") package;;
   "p") package;;
   "validate") validate;;
   "v") validate;;
   *) echo "Invalid cmd.  Use build, restore, clean, test, or layout";;
esac

popd
echo
echo Done.
echo
