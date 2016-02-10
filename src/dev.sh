
DEV_CMD=$1
LAYOUT_DIR=`pwd`/../_layout

define_os='OS_Windows'
BUILD_OS=`uname`
if [[ "$BUILD_OS" == 'Linux' ]]; then
   define_os='OS_Linux'
elif [[ "$BUILD_OS" == 'Darwin' ]]; then
   define_os='OS_OSX'
fi

build_config=${build_config}${target}
echo $build_config

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

# ---------- Pre-Reqs -----------

function heading()
{
    echo
    echo
    echo -------------------------------
    echo   ${1}
    echo -------------------------------
}

function build ()
{
    heading Building ...
    dotnet build Agent || failed "building Agent"
    dotnet build Worker || failed "building Worker"
    dotnet build Microsoft.VisualStudio.Services.Agent || failed "building lib"
    dotnet build Test || failed "building Test"    
}

function restore ()
{
    heading Restoring ...
    rm Agent/project.lock.json
    rm Microsoft.VisualStudio.Services.Agent/project.lock.json
    rm Test/project.lock.json
    rm Worker/project.lock.json    
    dotnet restore Agent || warn "restoring Agent"
    dotnet restore Worker || warn "restoring Worker"
    dotnet restore Microsoft.VisualStudio.Services.Agent || warn "restoring lib"
    dotnet restore Test || warn "restoring Test"
}

function cleanProj ()
{
    echo cleaning ${1}
    rm -rf `dirname ${0}`/${1}/bin
    rm -rf `dirname ${0}`/${1}/obj    
}

function clean ()
{
    heading Cleaning ...    
    cleanProj Agent
    cleanProj Worker
    cleanProj Microsoft.VisualStudio.Services.Agent
    cleanProj Test        
}

function publish ()
{
    heading Publishing ...
    dotnet publish Agent || failed "publish Agent"
    dotnet publish Worker || failed "publish Worker"
    dotnet publish Microsoft.VisualStudio.Services.Agent || failed "publish lib"
    dotnet publish Test || failed "publish Test"
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
