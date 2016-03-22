DEV_CMD=$1
DEV_SUBCMD=$2
LAYOUT_DIR=`pwd`/../_layout
DOWNLOAD_DIR=`pwd`/../_downloads

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
    echo -------------------------------
    echo   ${1}
    echo -------------------------------
}

function rundotnet ()
{
    dotnet_cmd=${1}
    err_handle=${2:-failed}
    run_dirs=("${!3}")
    heading ${1} ...
    
    for dir_name in ${run_dirs[@]}
    do
        echo
        echo -- Running: dotnet $dotnet_cmd $dir_name --
        echo
        dotnet ${dotnet_cmd} $dir_name || ${err_handle} "${dotnet_cmd} $dir_name"
    done   
}

function build ()
{
    rundotnet build failed build_dirs[@]
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
    pushd ${1}/bin/Debug/dnxcore50 > /dev/null
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
    update_dir=${DEV_SUBCMD} || failed "must specify directory to build and update"
    echo Updating ${update_dir}
    dotnet build ${update_dir} || failed "failed build"
    echo Publishing ${update_dir}
    dotnet publish ${update_dir} || failed "failed publish"
    copyBin ${update_dir}
}

function runtest ()
{
    heading Testing ...
    dotnet publish Test || failed "publishing Test"
    rm -Rf Test/bin/Debug/dnxcore50/_diag
    pushd Test/bin/Debug/dnxcore50 > /dev/null
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
    pkg_dir=`pwd`/../_package

    agent_ver=`${LAYOUT_DIR}/bin/Agent.Listener --version` || "failed version"
    agent_pkg_name="vsts-agent-${PLATFORM}-${agent_ver}-$(date +%m)$(date +%d).tar.gz"
    rm -Rf ${LAYOUT_DIR}/_diag
    mkdir -p $pkg_dir
    pushd $pkg_dir > /dev/null
    rm -Rf *
    tar -czf ${agent_pkg_name} -C ${LAYOUT_DIR} .
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
