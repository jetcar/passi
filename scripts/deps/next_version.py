"""Compute the next Android display version (major.minor, minor + 1) from `android-v<major>.<minor>` git tags.

Usage: git tag -l 'android-v*' | python next_version.py --base 1.57
"""
import argparse
import re
import sys

_TAG = re.compile(r"^android-v(\d+)\.(\d+)$")


def _parse(version):
    major, minor = version.split(".")
    return int(major), int(minor)


def next_version(tags, base):
    highest = _parse(base)
    for tag in tags:
        match = _TAG.match(tag.strip())
        if match:
            highest = max(highest, (int(match.group(1)), int(match.group(2))))
    return f"{highest[0]}.{highest[1] + 1}"


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("--base", required=True, help="Version to continue from when no higher tag exists")
    args = parser.parse_args()
    print(next_version(sys.stdin.read().split(), args.base))
