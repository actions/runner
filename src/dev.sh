DEV_CMD=$1
DEV_SUBCMD=$2
LAYOUT_DIR=`pwd`/../_layout
DOWNLOAD_DIR=`pwd`/../_downloads

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
if [[ "$PLATFORM" == 'linux' ]]; then
   define_os='OS_LINUX'
elif [[ "$PLATFORM" == 'darwin' ]]; then
   define_os='OS_OSX'
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
    run_dirs=("${!3}")

    heading ${1} ...

    cfg_args=""
    if [[ ("$dotnet_cmd" == "build") || ("$dotnet_cmd" == "publish") ]]; then
        cfg_args="-c ${BUILD_CONFIG}"
        echo "${cfg_args}"
    fi
    
    for dir_name in ${run_dirs[@]}
    do
        echo
        echo -- Running: dotnet $dotnet_cmd $dir_name --
        echo
        dotnet ${dotnet_cmd} $dir_name $cfg_args || ${err_handle} "${dotnet_cmd} $dir_name"
    done   
}

function build ()
{
    commit_token="_COMMIT_HASH_"
    commit_hash=`git rev-parse HEAD` || "failed git commit hash"
    echo "Building ${commit_hash}"
    sed "s/$commit_token/$commit_hash/g" "Misc/BuildConstants.ch" > "Microsoft.VisualStudio.Services.Agent/BuildConstants.cs"

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
    echo Generating project.json files ...
    for dir_name in ${build_dirs[@]}
    do
        rm $dir_name/project.json
        sed -e "s/OS_WINDOWS/$define_os/g" ./$dir_name/_project.json > ./$dir_name/project.json
    done

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
    pushd ${1}/bin/${BUILD_CONFIG}/dnxcore50 > /dev/null

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

    heading Externals ...
    bash ./Misc/externals.sh

    if [[ ("$PLATFORM" == "linux") || ("$PLATFORM" == "darwin") ]]; then
       package
    fi
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
        dotnet build ${update_dir} || failed "failed build"
        echo Publishing ${update_dir}
        dotnet publish ${update_dir} || failed "failed publish"
        copyBin ${update_dir}
    done
}

function runtest ()
{
    heading Testing ...
    dotnet publish Test || failed "publishing Test"
    rm -Rf Test/bin/${BUILD_CONFIG}/dnxcore50/_diag
    pushd Test/bin/${BUILD_CONFIG}/dnxcore50 > /dev/null
    pushd $(ls -d */ | grep -v '_')publish > /dev/null
    ./corerun xunit.console.netcore.exe Test.dll -xml testresults.xml || failed "failed tests"
    popd > /dev/null
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
    # if exist Agent.Listener/bin/${BUILD_CONFIG}/dnxcore50
    build_folder="Agent.Listener/bin/${BUILD_CONFIG}/dnxcore50"
    if [ ! -d "${build_folder}" ]; then
        echo "You must build first.  Expecting to find ${build_folder}"
    fi

    pushd "${build_folder}" > /dev/null
    pwd
    runtime_folder=`ls -d */`

    pkg_runtime=${runtime_folder%/}
    popd > /dev/null

    pkg_dir=`pwd`/../_package

    agent_ver=`${LAYOUT_DIR}/bin/Agent.Listener --version` || "failed version"
    agent_pkg_name="vsts-agent-${pkg_runtime}-${agent_ver}"
    # -$(date +%m)$(date +%d)"

    heading "Packaging ${agent_pkg_name}"

    rm -Rf ${LAYOUT_DIR}/_diag
    mkdir -p $pkg_dir
    pushd $pkg_dir > /dev/null
    rm -Rf *

    tar_name="${agent_pkg_name}.tar.gz"
    echo "Creating $tar_name in ${LAYOUT_DIR}"
    tar -czf "${tar_name}" -C ${LAYOUT_DIR} .
    
    # TODO: create package on windows with powershell.
    # zip -r "${agent_pkg_name}.zip" ${LAYOUT_DIR}
    popd > /dev/null
}

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

echo
echo Done.
echo
