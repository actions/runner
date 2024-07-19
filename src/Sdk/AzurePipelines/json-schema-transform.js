var fs = require("fs")
var content = fs.readFileSync("json-schema.json", { encoding: "utf8" })
var schema = JSON.parse(content)
var targetSchema = {}
targetSchema.version = "pipelines-v1.0"
targetSchema.definitions = {
    "workflow-key": {
      "context": [
        "parameters",
        "variables"
      ],
      "string": {}
    },

    "workflow-value": {
      "context": [
        "parameters",
        "variables"
      ],
      "one-of": [
        "boolean", "mapping", "null", "number", "sequence", "string"
      ]
    },
    "workflow-value-no-expand": {
      "context": [
        "no-expand"
      ],
      "one-of": [
        "boolean", "mapping", "null", "number", "sequence", "string"
      ]
    },

    "single-layer-workflow-mapping": {
      "context": [
        "parameters",
        "variables"
      ],
      "mapping": {
        "loose-key-type": "workflow-key",
        "loose-value-type": "workflow-value-no-expand"
      }
    },

    "single-layer-workflow-sequence": {
      "context": [
        "parameters",
        "variables"
      ],
      "sequence": {
        "item-type": "workflow-value-no-expand"
      }
    },
    "variable-result": {
      "context": [
        "variables",
        "dependencies",
        "stageDependencies",
        "pipeline",
        "resources",
        "Counter(0,2)"
      ],
      "string": {}
    },

    "job-if-result": {
      "context": [
        "variables",
        "dependencies",
        "stageDependencies",
        "Always(0,0)",
        "Canceled(0,0)",
        "Succeeded(0,MAX)",
        "SucceededOrFailed(0,MAX)",
        "Failed(0,MAX)"
      ],
      "boolean": {}
    },
    "stage-if-result": {
      "context": [
        "variables",
        "dependencies",
        "Always(0,0)",
        "Canceled(0,0)",
        "Succeeded(0,MAX)",
        "SucceededOrFailed(0,MAX)",
        "Failed(0,MAX)"
      ],
      "boolean": {}
    },

}
var stripDoNotSuggest = true
var defprefix = "#/definitions/";

var getId = function(id) {
    switch(id) {
        case "boolean":
        case "number":
        case "mapping":
        case "sequence":
        case "any":
        case "string":
            return "azp-" + id
        default:
            return id.replace(/[^A-Za-z0-9_-]/g, "_")
    }
}

var addDefinition = function(id) {
    var d = schema.definitions[id]
    var target = {}
    var deprecated = false
    if(d.type === "object" || d.properties) {
        target.mapping = {}
        target.mapping.properties = {}
        for(var pname in d.properties) {
            var pval = d.properties[pname]
            if(pval.doNotSuggest && stripDoNotSuggest) {
              deprecated = true
            }
            target.mapping.properties[pname] = {}
            var ref = pval["$ref"]
            if(ref && ref.indexOf(defprefix) === 0) {
                target.mapping.properties[pname].type = getId(ref.substring(defprefix.length))
            } else {
                schema.definitions[id + "-" + pname] = pval
                target.mapping.properties[pname].type = getId(id + "-" + pname)
                if(!addDefinition(id + "-" + pname)) {
                  deprecated = true
                }
            }
            if(d.firstProperty && d.firstProperty.includes(pname)) {
                target.mapping.properties[pname]["first-property"] = true
                target.mapping.properties[pname]["required"] = true
            }
            if(d.required && d.required.includes(pname)) {
                target.mapping.properties[pname]["required"] = true
            }
        }
        if(d.additionalProperties || !d.properties) {
            target.mapping["loose-key-type"] = "string"
            target.mapping["loose-value-type"] = "any"
        }
        targetSchema.definitions[getId(id)] = target
    } else if(d.type === "string") {
        // "referenceName": {
        //     "type": "string",
        //     "pattern": "^[-_A-Za-z0-9]*$"
        //   },
        target.string = {}
        if(d.pattern) {
            target.string.pattern = d.pattern
        }
        if(d.ignoreCase === "value" || d.ignoreCase === "all") {
            target.string["ignore-case"] = true
        }
        
        targetSchema.definitions[getId(id)] = target
    } else if(d.enum && d.enum.length === 1) {
        target.string = {}
        target.string.constant = d.enum[0]
        if(d.ignoreCase === "value" || d.ignoreCase === "all") {
          target.string["ignore-case"] = true
      }
        targetSchema.definitions[getId(id)] = target
    }  else if(d.enum) {
        target["allowed-values"] = []
        for(var val of d.enum) {
            target["allowed-values"].push(val)
        }
        targetSchema.definitions[getId(id)] = target
    } else if(d.anyOf) {
        target["one-of"] = []
        for(var i = 0; i < d.anyOf.length; i++) {
            var pval = d.anyOf[i]
            var ref = pval["$ref"]
            if(ref && ref.indexOf(defprefix) === 0) {
                target["one-of"].push(getId(ref.substring(defprefix.length)))
            } else {
                schema.definitions[id + "-" + i] = pval
                if(!addDefinition(id + "-" + i)) {
                  continue;
                }
                target["one-of"].push(getId(id + "-" + i))
            }
        }
        targetSchema.definitions[getId(id)] = target
    } else if(d.type === "array" || d.items) {
        target["sequence"] = {}
        if(d.items) {
            var pval = d.items
            var ref = pval["$ref"]
            if(ref && ref.indexOf(defprefix) === 0) {
                target["sequence"]["item-type"] = getId(ref.substring(defprefix.length))
            } else {
                schema.definitions[id + "-item"] = pval
                if(!addDefinition(id + "-item")) {
                  deprecated = true
                }
                target["sequence"]["item-type"] = getId(id + "-item")
            }
        }
        targetSchema.definitions[getId(id)] = target
    } else if(d.type === "boolean") {
        target["boolean"] = {}
        targetSchema.definitions[getId(id)] = target
    } else if(d.type === "integer") {
        target["number"] = {}
        targetSchema.definitions[getId(id)] = target
    }
    return !deprecated
}

for(var id in schema.definitions) {
    addDefinition(id)
}

var pathsWithExpressions = [
    [ ["pipeline", "variablesTemplate"], "variables" ],
    [ ["pipeline", "stagesTemplate"], "stages" ],
    [ ["pipeline", "jobsTemplate" ], "jobs" ],
    [ ["pipeline", "stepsTemplate"], "steps" ],
    [ "pipeline", "extends" ],
    [ "pipeline", "resources", "containers" ],
    [ "any_allowExpressions" ],
    [ "string_allowExpressions" ],
    [ "sequenceOfString_allowExpressions" ],
]

function * traverse(path) {
    var comp = path[0];
    if(comp instanceof Array) {
      for(var sk of comp) {
        var subPath = [...path]
        subPath[0] = sk
        for(var r of traverse(subPath)) {
          yield r;
        }
      }
    }
    var schema = targetSchema.definitions[comp];
    if(!schema) {
        return;
    }
    if(path.length === 1) {
        yield schema;
        return;
    }

    var oneof = schema["one-of"]
    if(oneof) {
        for(var entry of oneof) {
            var subPath = [...path]
            subPath[0] = entry
            for(var r of traverse(subPath)) {
                yield r;
            }
        }
    }
    var mapping = schema["mapping"]
    if(mapping) {
        for(var entry in mapping.properties) {
            var subPath = [...path]
            subPath.shift()
            if(entry === subPath[0]) {
                subPath[0] = mapping.properties[entry].type
                for(var r of traverse(subPath)) {
                    yield r;
                }
            }
        }
    }
}

for(var path of pathsWithExpressions) {
    for(var def of traverse(path)) {
        def.context = [ "parameters", "variables" ]
    }
}

targetSchema.definitions["pipeline-root"] = targetSchema.definitions["pipeline"]
targetSchema.definitions["variable-template-root"] = targetSchema.definitions["variablesTemplate"]
targetSchema.definitions["step-template-root"] = targetSchema.definitions["stepsTemplate"]
targetSchema.definitions["job-template-root"] = targetSchema.definitions["jobsTemplate"]
targetSchema.definitions["stage-template-root"] = targetSchema.definitions["stagesTemplate"]

targetSchema.definitions["containerArtifactType"] = {
    "one-of": [
        "containerArtifactType-1"
    ]
}

targetSchema.definitions["nonEmptyString"] = {
  "string": {
      "require-non-empty": true
  }
},

targetSchema.definitions["task-task"] = targetSchema.definitions["nonEmptyString"]

// delete targetSchema.definitions["any"]

targetSchema.definitions["azp-any"]["one-of"].push("boolean", "null", "number")
targetSchema.definitions["any_allowExpressions"]["one-of"].push("boolean", "null", "number")

console.log(JSON.stringify(targetSchema, null, 4))