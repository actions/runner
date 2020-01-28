# ADR 0297: Base64 Masking Trailing Characters

**Date** 2020-01-21

**Status** Proposed

## Context

The Runner registers a number of Value Encoders, which mask various encodings of a provided secret. Currently, we register a 3 base64 Encoders:
- The base64 encoded secret
- The secret with the first character removed then base64 encoded
- The secret with the first two characters removed then base64 encoded

This gives us good coverage across the board for secrets and secrets with a prefix (i.e. `base64($user:$pass)`).

However, we don't have great coverage for cases where the secret has a string appended to it before it is base64 encoded (i.e.: `base64($pass\n))`). 

Most notably we've seen this as a result of user error where a user accidentially appends a newline or space character before encoding their secret in base64.

## Decision

### Trim end characters

We are going to modify all existing base64 encoders to trim information before registering as a secret.
We will trim:
- `=` from the end of all base64 strings. This is a padding character that contains no information. 
  - Based on the number of `=`'s at the end of a base64 string, a malicious user could predict the length of the original secret modulo 3. 
    - If a user saw `***==`, they would know the secret could be 1,4,7,10... characters.
- If a string contains `=` we will also trim the last non-padding character from the base64 secret.
  - This character can change if a string is appended to the secret before the encoding.


### Register a fourth encoder

We will also add back in the original base64 encoded secret encoder for four total encoders:
- The base64 encoded secret
- The base64 encoded secret trimmed
- The secret with the first character removed then base64 encoded and trimmed
- The secret with the first two characters removed then base64 encoded and trimmed

This allows us to fully cover the most common scenario where a user base64 encodes their secret and expects the entire thing to be masked.
This will result in us only revealing length or bit information when a prefix or suffix is added to a secret before encoding. 

## Consequences

- In the case where a secret has a prefix or suffix added before base64 encoding, we may now reveal up to 20 bits of information and the length of the original string modulo 3, rather then the original 16 bits and no length information
- Secrets with a suffix appended before encoding will now be masked across the board. Previously it was only masked if it was a multiple of 3 characters
- Performance will suffer in a neglible way
