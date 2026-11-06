#!/usr/bin/env bash
set -euo pipefail

app_path="${1:?Usage: package-ipa.sh /path/to/Passi.app [/path/to/Passi-unsigned.ipa]}"
output_path="${2:-Passi-unsigned.ipa}"

if [[ ! -d "$app_path" ]]; then
  echo "Application bundle not found: $app_path" >&2
  exit 1
fi

info_plist="$app_path/Info.plist"
if [[ ! -f "$info_plist" ]]; then
  echo "Application Info.plist not found" >&2
  exit 1
fi

executable_name="$(/usr/libexec/PlistBuddy -c 'Print :CFBundleExecutable' "$info_plist")"
for required_path in "$app_path/$executable_name" "$app_path/Assets.car"; do
  if [[ ! -e "$required_path" ]]; then
    echo "Required application file not found: $required_path" >&2
    exit 1
  fi
done

icon_name="$(/usr/libexec/PlistBuddy -c 'Print :CFBundleIcons:CFBundlePrimaryIcon:CFBundleIconName' "$info_plist")"
if [[ "$icon_name" != "AppIcon" ]]; then
  echo "Application icon catalog is not configured" >&2
  exit 1
fi

output_directory="$(dirname "$output_path")"
mkdir -p "$output_directory"
output_directory="$(cd "$output_directory" && pwd)"
output_path="$output_directory/$(basename "$output_path")"

package_root="$(mktemp -d)"
trap 'rm -rf "$package_root"' EXIT

mkdir -p "$package_root/Payload"
/usr/bin/ditto "$app_path" "$package_root/Payload/Passi.app"
(
  cd "$package_root"
  /usr/bin/ditto -c -k --sequesterRsrc --keepParent Payload "$output_path"
)

echo "$output_path"
