
DEV_CMD=$1
DEV_SUBCMD=$2
LAYOUT_DIR=`pwd`/../_layout
AGENTSERVICE_SRC=`pwd`/Agent.Service

define_os='OS_WINDOWS'
BUILD_OS=`uname`
if [[ "$BUILD_OS" == 'Linux' ]]; then
   define_os='OS_LINUX'
elif [[ "$BUILD_OS" == 'Darwin' ]]; then
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

function buildagentservice()
{
    if [[ "$define_os" == 'OS_LINUX' ]]; then
	echo Building Linux Agent Service ...

	#TODO Assume node/gulp already installed?
	pushd ${AGENTSERVICE_SRC}/Linux
	gulp build
	popd
    fi
}

function deployagentservice()
{
    if [[ "$define_os" == 'OS_LINUX' ]]; then
	echo Building and deploying Linux Agent Service ...

	#TODO Assume node/gulp already installed?
	pushd ${AGENTSERVICE_SRC}/Linux > /dev/null
	gulp layout
	popd > /dev/null

	# fetch run time dependency, eventually this will also be part of packaging (getagent.sh)
	pushd ${LAYOUT_DIR}/bin > /dev/null
	npm install
	popd > /dev/null
    fi
}

function build ()
{
    rundotnet build failed build_dirs[@]
    buildagentservice
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
    rm -rf ${LAYOUT_DIR}
    mkdir -p ${LAYOUT_DIR}/bin
    for bin_copy_dir in ${bin_layout_dirs[@]}
    do
        copyBin ${bin_copy_dir}
    done
    
    cp -Rf ./Misc/layoutroot/* ${LAYOUT_DIR}
    cp -Rf ./Misc/layoutbin/* ${LAYOUT_DIR}/bin

    deployagentservice
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
    ./corerun xunit.console.netcore.exe Test.dll -xml testresults.xml
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
   "validate") validate;;
   "v") validate;;
   *) echo "Invalid cmd.  Use build, restore, clean, test, or layout";;
esac

echo
echo Done.
echo
