NODE_VERSION="4.3.2"

get_abs_path() {
  # exploits the fact that pwd will print abs path when no args
  echo "$(cd "$(dirname "$1")" && pwd)/$(basename "$1")"
}

LAYOUT_DIR=$(get_abs_path `dirname $0`/../../_layout)
DOWNLOAD_DIR=$(get_abs_path `dirname $0`/../../_downloads)

PLATFORM_NAME=`uname`
PLATFORM="windows"
if [[ ("$PLATFORM_NAME" == "Linux") || ("$PLATFORM_NAME" == "Darwin") ]]; then
   PLATFORM=`echo "${PLATFORM_NAME}" | awk '{print tolower($0)}'`
fi

function checkRC() {
    local rc=$?
    if [ $rc -ne 0 ]; then
        failed "${1} Failed with return code $rc"
    fi
}

function acquireNode ()
{
    echo "Downloading Node ${NODE_VERSION}..."
    target_dir="${LAYOUT_DIR}/externals/node"
    if [ -d $target_dir ]; then
        rm -Rf $target_dir
    fi
    mkdir -p $target_dir

    mkdir -p "${DOWNLOAD_DIR}"
    pushd "${DOWNLOAD_DIR}" > /dev/null

    if [[ "$PLATFORM" == "windows" ]]; then
        
        # Windows

        node_download_dir="${DOWNLOAD_DIR}/node-v${NODE_VERSION}-win-x64"
        mkdir -p $node_download_dir
        pushd "${node_download_dir}" > /dev/null

        node_exe_url=https://nodejs.org/dist/v${NODE_VERSION}/win-x64/node.exe
        echo "Downloading Node ${NODE_VERSION} @ ${node_exe_url}"
        curl -kSLO $node_exe_url &> "./node_download_exe.log"
        node_lib_url=https://nodejs.org/dist/v${NODE_VERSION}/win-x64/node.lib
        curl -kSLO $node_lib_url &> "./node_download_lib.log"
        checkRC "Download (curl)"

        echo "Copying to layout"
        mkdir ${target_dir}/bin
        cp -f node.* ${target_dir}/bin
        
        popd > /dev/null
    else

        # OSX/Linux

        node_file="node-v${NODE_VERSION}-${PLATFORM}-x64"
        node_zip="${node_file}.tar.gz"
        
        if [ -f ${node_zip} ]; then
            echo "Download exists"
        else
            node_url="https://nodejs.org/dist/v${NODE_VERSION}/${node_zip}"
            echo "Downloading Node ${NODE_VERSION} @ ${node_url}"
            curl -kSLO $node_url &> "./node_download.log"
            checkRC "Download (curl)"
        fi

        if [ -d ${node_file} ]; then
            echo "Already extracted"
        else
            echo "Extracting"
            tar zxvf ${node_zip} &> "node_tar.log"
            checkRC "Unzip (node)"
        fi   
        
        # copy to layout
        echo "Copying to layout"
        cp -Rf ${node_file}/* ${target_dir} 
    fi    

    popd > /dev/null

    echo Done
}

acquireNode
