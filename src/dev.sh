NODE_VERSION="4.3.2"

DEV_CMD=$1
DEV_SUBCMD=$2
LAYOUT_DIR=`pwd`/../_layout
DOWNLOAD_DIR=`pwd`/../_downloads

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
    cp -Rf $(ls -d */) ${LAYOUT_DIR}/bin
    popd > /dev/null 
}

function downloadNode ()
{
    echo "Downloading Node ${NODE_VERSION}..."
    target_dir="${LAYOUT_DIR}/externals/node"
    mkdir -p $target_dir

    if [[ "$define_os" == 'OS_WINDOWS' ]]; then
        # Windows
        node_download_dir="${DOWNLOAD_DIR}/node-v${NODE_VERSION}-win-x64"

        pushd "${node_download_dir}" > /dev/null
        node_exe_url=https://nodejs.org/dist/v${NODE_VERSION}/win-x64/node.exe

        # TODO finish on win
        
        popd > /dev/null
    else
        # OSX/Linux
        mkdir -p "${DOWNLOAD_DIR}"
        pushd "${DOWNLOAD_DIR}" > /dev/null

        platform_lower=`echo "${BUILD_OS}" | awk '{print tolower($0)}'`
        node_file="node-v${NODE_VERSION}-${platform_lower}-x64"
        node_zip="${node_file}.tar.gz"
        
        if [ -f ${node_zip} ]; then
            echo "Download exists"
        else
            node_url="https://nodejs.org/dist/v${NODE_VERSION}/${node_zip}"
            echo "Downloading Node ${NODE_VERSION} @ ${node_url}"
            curl -skSLO $node_url &> "${DOWNLOAD_DIR}/curl.log"
            checkRC "Download (curl)"
        fi

        if [ -d ${node_file} ]; then
            echo "Already extracted"
        else
            echo "Extracting"
            tar zxvf ${node_zip} &> "${DOWNLOAD_DIR}/tar.log"
            checkRC "Unzip (node)"
        fi   
        
        # copy to layout
        echo "Copying to layout"
        cp -R ${node_file}/* ${target_dir}    

        popd > /dev/null
    fi    

    echo Done

    
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
    pushd $(ls -d */) > /dev/null
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
   "n") downloadNode;;
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
