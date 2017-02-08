
# Task Conditions

## Overview

Team build provides users with the capability to create a single definition with multiple different triggers. Depending on the trigger configuration those triggers my also build many different branches. This capability has solved some of the scenarios where customers of Xaml build had to create and maintain multiple build definitions, however, it still falls short in a couple of key ways.
 
One specific example is the ability to have certain triggers do more or less work in a given build. It is very common for a user to want to configure their CI build to be very fast and run a minimal set of tests while having a nightly scheduled build run a larger set of tests. Currently the only option the user has is to fall back on writing a script to run their tests and then check the BUILD_REASON environment variable. While this is a work around is does reduce the overall usefulness of our CI system.
 
To improve on the scenario of having a single build definition that builds multiple different triggers and branches we can introduce the concept of a Conditional on each task and phase that will be evaluated first on the server and then on the agent. In the case of the server evaluation a negative condition acts in the same way a disabled task would and it is removed from the job before it is sent to the agent.

## Expression syntax

The UI can provide for an editor but the expression should be stored in a simple syntax that will be easily compatible with config as code scenarios.  The syntax will simply be a nested set of functions that are evaluated starting with the inner function and working its way out.  All expressions must ultimately result in a boolean. At a later date we could easily add additional evaluators to the expression for string parsing and other operations. 

Example: Run a step only for the master branch
`eq(variables('Source.BranchName'), 'master')`

Example: Run a task for all branches other than master
`ne(variables('Source.BranchName'), 'master')`

`succeeded()`

<!--`capabilities('node')`-->

## UX notes
`Runs on` dropdown with following options:
* Success
* Succes or failed
* Always (note, includes canceled)
* Condition
 - When condition is selected, a condition builder area becomes visible. The customer has three inputs: Variable, Operator, Value. And a plus button to add additional conditions.
 - Conditions are and'ed
 - TODO: Implicitly wrap `and(success(), ...)` and don't show `agent.jobstatus` in the dropdown?
 - TODO: Wrap `Value` in single-quotes, unless already contains a single-quote? (required to enable `in`)
* Custom
 - When custom is selected, a text area becomes visible.
 - ~~~Evaluation will implicitly wrap `and(success(), ...)` if agent.jobstatus variable or job status functions not referenced.~~~ Coalesce with `success()` when custom condition is selected and the text area is left empty.

## Open issues
* Need to determine whether variable macro expansion is supported within the expression. Matters for how rules are applied in the future w.r.t. inline expressions.

## Technical reference

### Types

#### Boolean
`true` or `false` (ordinal case insensitive)

#### Null
Null is a special type that is returned from a dictionary miss only, e.g. (`variables('noSuch')`). There is no keyword for null.

#### Number
Starts with `-` `.` or `0-9`. Internally parses into .Net `Decimal` type using invariant culture.

Cannot contain `,` since it is a separator.

#### String
Single-quoted, e.g. 'this is a string' or ''

Literal single-quote escaped by two single quotes, e.g. 'all y''all'

#### Version
Starts with a number and contains two or three `.`. Internall parses into .Net `Version` type.

Note, only one `.` is present, then the value would be parsed as a number.

<!--#### Object
Pre-defined complex objects are available depending on the context.

##### Within agent context
A `variables` function is available. The variables function contains String values only. See `variables` function technical reference below.

##### Within server context
TODO: need more details on this section

A `capabilities` function is available. The capabilities dictionary contains String values only.

Other complex orchestration-state object are available (String=\>Any), which may contain nested objects). Note, `Any` may be one of any supported type: Boolean, Number, Version, String, or Dictionary(String=\>Any)

##### Syntax to access values
The first level value will always be accessed using function syntax. For example: `variables('Agent.JobStatus')`

Beyond the first level, two syntaxes are supported for accessing further levels.
* Index syntax - `someComplexObject('firstLevel')['secondLevel']`
* Property dereference syntax - `someComplexObject('firstLevel').secondLevel`
 - In order to use the property dereference syntax, the property name must adhere to the regex `^[a-zA-Z_][a-zA-Z0-9_]*$`

Examples for complex objects:
* Chaining accessors: `someComplexObject('firstLevel')['secondLevel'].thirdLevel`
* Nested evaluation: `someComplexObject(anotherObject('someProperty'))`

##### Accessor rules
* When an accessor is applied against a dictionary and the key does not exist, null is returned.
* When an accessor is applied against a non-dictionary object (including null), null is returned.
 - This means that `someObject('noSuchKey').alsoNoSuchKey.alsoAlsoNoSuchKey` will simply return null.
-->

### Type Casting

#### Conversion chart
Detailed conversion rules are listed further below.

|          |             | To          |             |             |             |             |             |             |
| -------- | ----------- | ----------- | ----------- | ----------- | ----------- | ----------- | ----------- | ----------- |
|          |             | **Array**   | **Boolean** | **Null**    | **Number**  | **Object**  | **String**  | **Version** |
| **From** | **Array**   | -           | Yes         | -           | -           | -           | -           | -           |
|          | **Boolean** | -           | -           | -           | Yes         | -           | Yes         | -           |
|          | **Null**    | -           | Yes         | -           | Yes         | -           | Yes         | -           |
|          | **Number**  | -           | Yes         | -           | -           | -           | Yes         | Partial     |
|          | **Object**  | -           | Yes         | -           | -           | -           | -           | -           |
|          | **String**  | -           | Yes         | Partial     | Partial     | -           | -           | Partial     |
|          | **Version** | -           | Yes         | -           | -           | -           | Yes         | -           |

Note, Array and Object not currently exposed via any expressions available on the agent

#### Array to Boolean
* =\> True

#### Boolean to Number
* False =\> 0
* True =\> 1

#### Boolean to String
* False =\> 'False'
* True =\> 'True'

#### Null to Boolean
* =\> False

#### Null to Number
* =\> 0

#### Null to String
* =\> Empty string

#### Number to Boolean
* 0 =\> False
* Otherwise =\> True

#### Number to Version
* Must be greater than zero and must contain a non-zero decimal. Must be less than Int32.MaxValue (decimal component also).

#### Number to String
* =\> Invariant-culture ToString

#### Object to Boolean
* =\> True

#### String to Boolean
* Empty string =\> False
* Otherwise =\> True

#### String to Null
* Empty string =\> Null
* Otherwise not convertible

#### String to Number
* Empty string =\> 0
* Otherwise try-parse using invariant-culture and the following rules: AllowDecimalPoint | AllowLeadingSign | AllowLeadingWhite | AllowThousands | AllowTrailingWhite. If try-parse fails, then not convertible.

#### String to Version
* Try-parse. Must contain Major and Minor component at minimum. If try-parse fails, then not convertible.

#### Version to Boolean
* =\> True

#### Version to String
* Major.Minor
* or Major.Minor.Build
* or Major.Minor.Build.Revision

### Functions

#### and
* Evaluates True if all parameters are True
* Min parameters: 2. Max parameters: N
* Casts parameters to Boolean for evaluation
* Short-circuits after first False

#### contains
* Evaluates True if left parameter String contains right parameter
* Min parameters: 2. Max parameters: 2
* Casts parameters to String for evaluation
* Performs ordinal ignore-case comparison

#### endsWith
* Evaluates True if left parameter String ends with right parameter
* Min parameters: 2. Max parameters: 2
* Casts parameters to String for evaluation
* Performs ordinal ignore-case comparison

#### eq
* Evaluates True if parameters are equal
* Min parameters: 2. Max parameters: 2
* Converts right parameter to match type of left parameter. Returns False if conversion fails.
* Ordinal ignore-case comparison for Strings

#### ge
* Evaluates True if left parameter is greater than or equal to the right parameter
* Min parameters: 2. Max parameters: 2
* Converts right parameter to match type of left parameter. Errors if conversion fails.
* Ordinal ignore-case comparison for Strings

#### gt
* Evaluates True if left parameter is greater than the right parameter
* Min parameters: 2. Max parameters: 2
* Converts right parameter to match type of left parameter. Errors if conversion fails.
* Ordinal ignore-case comparison for Strings

#### in
* Evaluates True if left parameter is equal to any right parameter
* Min parameters: 1. Max parameters: N
* Converts right parameters to match type of left parameter. Equality comparison evaluates False if conversion fails.
* Ordinal ignore-case comparison for Strings
* Short-circuits after first match

#### le
* Evaluates True if left parameter is less than or equal to the right parameter
* Min parameters: 2. Max parameters: 2
* Converts right parameter to match type of left parameter. Errors if conversion fails.
* Ordinal ignore-case comparison for Strings

#### lt
* Evaluates True if left parameter is less than the right parameter
* Min parameters: 2. Max parameters: 2
* Converts right parameter to match type of left parameter. Errors if conversion fails.
* Ordinal ignore-case comparison for Strings

#### ne
* Evaluates True if parameters are not equal
* Min parameters: 2. Max parameters: 2
* Converts right parameter to match type of left parameter. Returns True if conversion fails.
* Ordinal ignore-case comparison for Strings

#### not
* Evaluates True if parameter is False
* Min parameters: 1. Max parameters: 1
* Converts value to Boolean for evaluation

#### notIn
* Evaluates True if left parameter is not equal to any right parameter
* Min parameters: 1. Max parameters: N
* Converts right parameters to match type of left parameter. Equality comparison evaluates False if conversion fails.
* Ordinal ignore-case comparison for Strings
* Short-circuits after first match

#### or
* Evaluates True if any parameter is true
* Min parameters: 2. Max parameters: N
* Casts parameters to Boolean for evaluation
* Short-circuits after first True

#### startsWith
* Evaluates true if left parameter string starts with right parameter
* Min parameters: 2. Max parameters: 2
* Casts parameters to String for evaluation
* Performs ordinal ignore-case comparison

#### xor
* Evaluates True if exactly one parameter is True
* Min parameters: 2. Max parameters: 2
* Casts parameters to Boolean for evaluation

#### variables
* Returns a variable or Null if not found. For example: `variables('Build.Reason')`
* Min parameters: 1. Max parameters: 1
* Casts parameter to String for evaluation

#### succeeded
* Evaluates True when `in(variables('Agent.JobStatus'), 'Succeeded', 'PartiallySucceeded')`
* Min parameters: 0. Max parameters: 0

#### succeededOrFailed
* Evaluates True when `in(variables('Agent.JobStatus'), 'Succeeded', 'PartiallySucceeded', 'Failed')`
* Min parameters: 0. Max parameters: 0

#### always
* Evaluates True when `in(variables('Agent.JobStatus'), 'Succeeded', 'PartiallySucceeded', 'Failed', 'Canceled')`. Note, critical-failure may still prevent a task from running - e.g. get sources plugin failed.
* Min parameters: 0. Max parameters: 0
