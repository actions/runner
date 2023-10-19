export var basePaths = {};
export var customImports = {};
export async function myimport(url) {
    return customImports[url];
}