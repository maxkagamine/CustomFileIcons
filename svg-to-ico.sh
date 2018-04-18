#!/usr/bin/env bash
set -e

if [[ $# == 0 ]]; then
	echo 'Drop one or more svgs onto the script.'
	echo 'Press enter to exit.'
	read
	exit
fi

for path in "$@"; do

	echo "$(basename "$path")"

	if [[ ! -f $path || $path != *.svg ]]; then
		echo 'Not an svg.'
		continue
	fi

	basename=$(basename $path .svg | sed 's/^file_type_//')

	printf 'Name: [%s] ' "$basename"
	read name
	[[ -z $name ]] && name=$basename

	echo 'Generating ico...'
	node ./gen "$path" "$name"
	echo

done

echo 'Press enter to exit.'
read
