export var basePaths = {};
export var customImports = {};
export async function myimport(url) {
    console.log("fake-import: " + url);
    return customImports[url];
}