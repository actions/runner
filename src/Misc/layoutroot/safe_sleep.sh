#!/bin/bash

safe_sleep() {
    local t="$1"

    if command -v sleep >/dev/null 2>&1; then
        sleep "$t" && return 0
    fi

    if read -t "$t" <> <(:) 2>/dev/null; then
        return 0
    fi

    if command -v perl >/dev/null 2>&1; then
        perl -e "select(undef, undef, undef, $t);" && return 0
    fi

    local end
    end=$(printf "%.0f" "$(echo "$(date +%s.%3N) + $t" | bc)")
    while (( $(date +%s) < end )); do
        sleep 0.05  
    done

    return 0
}

safe_sleep "$1"
