
DEV_CMD=$1
LAYOUT_DIR=`pwd`/../_layout

define_os='OS_WINDOWS'
BUILD_OS=`uname`
if [[ "$BUILD_OS" == 'Linux' ]]; then
   define_os='OS_LINUX'
elif [[ "$BUILD_OS" == 'Darwin' ]]; then
   define_os='OS_OSX'
fi

build_dirs=("Agent" "Microsoft.VisualStudio.Services.Agent" "Test" "Worker")

echo Generating project.json files ...
for dir_name in ${build_dirs[@]}
do
    rm $dir_name/project.json
    sed -e "s/OS_WINDOWS/$define_os/g" ./$dir_name/_project.json > ./$dir_name/project.json
done

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
    heading ${1} ...
    for dir_name in ${build_dirs[@]}
    do
        dotnet ${dotnet_cmd} $dir_name || ${err_handle} "${dotnet_cmd} $dir_name"
    done   
}

function build ()
{
    rundotnet build failed
}

function restore ()
{
    rundotnet restore warn
}

function clean ()
{
    heading Cleaning ...
    for dir_name in ${build_dirs[@]}
    do
        echo Cleaning ${dir_name} ...
        rm -rf `dirname ${0}`/${dir_name}/bin
        rm -rf `dirname ${0}`/${dir_name}/obj
    done
}

function publish ()
{
    rundotnet publish
}

function copyProj ()
{
    echo Copying ${1}
    pushd ${1}/bin/Debug/dnxcore50 > /dev/null
    cp -Rf $(ls -d */) ${LAYOUT_DIR}
    popd > /dev/null 
}

function layout ()
{
    clean
    restore
    build
    publish
    rm -rf ${LAYOUT_DIR}
    mkdir -p ${LAYOUT_DIR}
    copyProj Agent
    copyProj Worker
    copyProj Microsoft.VisualStudio.Services.Agent 
}

function test ()
{
    heading Testing ...
    dotnet publish Test || failed "publishing Test"
    pushd Test/bin/Debug/dnxcore50 > /dev/null
    pushd $(ls -d */) > /dev/null
    ./corerun xunit.console.netcore.exe Test.dll -xml testresults.xml
    popd > /dev/null
    popd > /dev/null
}

function buildtest ()
{
    build
    test
}

case $DEV_CMD in
   "build") build;;
   "b") build;;
   "test") test;;
   "t") test;;
   "bt") buildtest;;   
   "clean") clean;;
   "c") clean;;
   "restore") restore;;
   "r") restore;;
   "layout") layout;;
   "l") layout;;
   *) echo "Invalid cmd.  Use build, restore, clean, test, or layout";;
esac

echo
echo Done.
echo
