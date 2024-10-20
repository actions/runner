export var basePaths = {};
export var customImports = {
    module: {
        createRequire: (base) => {
            return (url) => {
                return customImports[url] || __non_webpack_require__(url);
            }
        }
    }
};
export async function myimport(url) {
    return customImports[url];
}
