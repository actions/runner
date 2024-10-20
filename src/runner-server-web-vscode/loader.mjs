import * as path from "path"

export default function (source) {
  return `
    import { basePaths, myimport } from ${JSON.stringify(path.resolve(process.cwd(), "server/src/config.js"))};
    ${source.replace(/import\.meta\.url/g, "basePaths.basedir").replace(/import\(([^\)"']+)\)/g, "myimport($1)").replace(/import\("module"\)/g, "myimport(\"module\")")}`;
}
