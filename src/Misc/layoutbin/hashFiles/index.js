/******/ (() => { // webpackBootstrap
/******/ 	"use strict";
/******/ 	var __webpack_modules__ = ({

/***/ 711:
/***/ (function(__unused_webpack_module, exports, __nccwpck_require__) {


var __createBinding = (this && this.__createBinding) || (Object.create ? (function(o, m, k, k2) {
    if (k2 === undefined) k2 = k;
    var desc = Object.getOwnPropertyDescriptor(m, k);
    if (!desc || ("get" in desc ? !m.__esModule : desc.writable || desc.configurable)) {
      desc = { enumerable: true, get: function() { return m[k]; } };
    }
    Object.defineProperty(o, k2, desc);
}) : (function(o, m, k, k2) {
    if (k2 === undefined) k2 = k;
    o[k2] = m[k];
}));
var __setModuleDefault = (this && this.__setModuleDefault) || (Object.create ? (function(o, v) {
    Object.defineProperty(o, "default", { enumerable: true, value: v });
}) : function(o, v) {
    o["default"] = v;
});
var __importStar = (this && this.__importStar) || (function () {
    var ownKeys = function(o) {
        ownKeys = Object.getOwnPropertyNames || function (o) {
            var ar = [];
            for (var k in o) if (Object.prototype.hasOwnProperty.call(o, k)) ar[ar.length] = k;
            return ar;
        };
        return ownKeys(o);
    };
    return function (mod) {
        if (mod && mod.__esModule) return mod;
        var result = {};
        if (mod != null) for (var k = ownKeys(mod), i = 0; i < k.length; i++) if (k[i] !== "default") __createBinding(result, mod, k[i]);
        __setModuleDefault(result, mod);
        return result;
    };
})();
var __awaiter = (this && this.__awaiter) || function (thisArg, _arguments, P, generator) {
    function adopt(value) { return value instanceof P ? value : new P(function (resolve) { resolve(value); }); }
    return new (P || (P = Promise))(function (resolve, reject) {
        function fulfilled(value) { try { step(generator.next(value)); } catch (e) { reject(e); } }
        function rejected(value) { try { step(generator["throw"](value)); } catch (e) { reject(e); } }
        function step(result) { result.done ? resolve(result.value) : adopt(result.value).then(fulfilled, rejected); }
        step((generator = generator.apply(thisArg, _arguments || [])).next());
    });
};
var __asyncValues = (this && this.__asyncValues) || function (o) {
    if (!Symbol.asyncIterator) throw new TypeError("Symbol.asyncIterator is not defined.");
    var m = o[Symbol.asyncIterator], i;
    return m ? m.call(o) : (o = typeof __values === "function" ? __values(o) : o[Symbol.iterator](), i = {}, verb("next"), verb("throw"), verb("return"), i[Symbol.asyncIterator] = function () { return this; }, i);
    function verb(n) { i[n] = o[n] && function (v) { return new Promise(function (resolve, reject) { v = o[n](v), settle(resolve, reject, v.done, v.value); }); }; }
    function settle(resolve, reject, d, v) { Promise.resolve(v).then(function(v) { resolve({ value: v, done: d }); }, reject); }
};
Object.defineProperty(exports, "__esModule", ({ value: true }));
const crypto = __importStar(__nccwpck_require__(982));
const fs = __importStar(__nccwpck_require__(896));
const glob = __importStar(__nccwpck_require__(791));
const path = __importStar(__nccwpck_require__(928));
const stream = __importStar(__nccwpck_require__(203));
const util = __importStar(__nccwpck_require__(23));
function run() {
    return __awaiter(this, void 0, void 0, function* () {
        var _a, e_1, _b, _c;
        // arg0 -> node
        // arg1 -> hashFiles.js
        // env[followSymbolicLinks] = true/null
        // env[patterns] -> glob patterns
        let followSymbolicLinks = false;
        const matchPatterns = process.env.patterns || '';
        if (process.env.followSymbolicLinks === 'true') {
            console.log('Follow symbolic links');
            followSymbolicLinks = true;
        }
        console.log(`Match Pattern: ${matchPatterns}`);
        let hasMatch = false;
        const githubWorkspace = process.cwd();
        const result = crypto.createHash('sha256');
        let count = 0;
        const globber = yield glob.create(matchPatterns, { followSymbolicLinks });
        try {
            for (var _d = true, _e = __asyncValues(globber.globGenerator()), _f; _f = yield _e.next(), _a = _f.done, !_a; _d = true) {
                _c = _f.value;
                _d = false;
                const file = _c;
                console.log(file);
                if (!file.startsWith(`${githubWorkspace}${path.sep}`)) {
                    console.log(`Ignore '${file}' since it is not under GITHUB_WORKSPACE.`);
                    continue;
                }
                if (fs.statSync(file).isDirectory()) {
                    console.log(`Skip directory '${file}'.`);
                    continue;
                }
                const hash = crypto.createHash('sha256');
                const pipeline = util.promisify(stream.pipeline);
                yield pipeline(fs.createReadStream(file), hash);
                result.write(hash.digest());
                count++;
                if (!hasMatch) {
                    hasMatch = true;
                }
            }
        }
        catch (e_1_1) { e_1 = { error: e_1_1 }; }
        finally {
            try {
                if (!_d && !_a && (_b = _e.return)) yield _b.call(_e);
            }
            finally { if (e_1) throw e_1.error; }
        }
        result.end();
        if (hasMatch) {
            console.log(`Found ${count} files to hash.`);
            console.error(`__OUTPUT__${result.digest('hex')}__OUTPUT__`);
        }
        else {
            console.error(`__OUTPUT____OUTPUT__`);
        }
    });
}
;
(() => __awaiter(void 0, void 0, void 0, function* () {
    try {
        const out = yield run();
        console.log(out);
        process.exit(0);
    }
    catch (err) {
        console.error(err);
        process.exit(1);
    }
}))();


/***/ }),

/***/ 791:
/***/ ((module) => {

module.exports = require("@actions/glob");

/***/ }),

/***/ 982:
/***/ ((module) => {

module.exports = require("crypto");

/***/ }),

/***/ 896:
/***/ ((module) => {

module.exports = require("fs");

/***/ }),

/***/ 928:
/***/ ((module) => {

module.exports = require("path");

/***/ }),

/***/ 203:
/***/ ((module) => {

module.exports = require("stream");

/***/ }),

/***/ 23:
/***/ ((module) => {

module.exports = require("util");

/***/ })

/******/ 	});
/************************************************************************/
/******/ 	// The module cache
/******/ 	var __webpack_module_cache__ = {};
/******/ 	
/******/ 	// The require function
/******/ 	function __nccwpck_require__(moduleId) {
/******/ 		// Check if module is in cache
/******/ 		var cachedModule = __webpack_module_cache__[moduleId];
/******/ 		if (cachedModule !== undefined) {
/******/ 			return cachedModule.exports;
/******/ 		}
/******/ 		// Create a new module (and put it into the cache)
/******/ 		var module = __webpack_module_cache__[moduleId] = {
/******/ 			// no module.id needed
/******/ 			// no module.loaded needed
/******/ 			exports: {}
/******/ 		};
/******/ 	
/******/ 		// Execute the module function
/******/ 		var threw = true;
/******/ 		try {
/******/ 			__webpack_modules__[moduleId].call(module.exports, module, module.exports, __nccwpck_require__);
/******/ 			threw = false;
/******/ 		} finally {
/******/ 			if(threw) delete __webpack_module_cache__[moduleId];
/******/ 		}
/******/ 	
/******/ 		// Return the exports of the module
/******/ 		return module.exports;
/******/ 	}
/******/ 	
/************************************************************************/
/******/ 	/* webpack/runtime/compat */
/******/ 	
/******/ 	if (typeof __nccwpck_require__ !== 'undefined') __nccwpck_require__.ab = __dirname + "/";
/******/ 	
/************************************************************************/
/******/ 	
/******/ 	// startup
/******/ 	// Load entry module and return exports
/******/ 	// This entry module is referenced by other modules so it can't be inlined
/******/ 	var __webpack_exports__ = __nccwpck_require__(711);
/******/ 	module.exports = __webpack_exports__;
/******/ 	
/******/ })()
;