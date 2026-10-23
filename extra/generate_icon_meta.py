#!/usr/bin/env python3

# Copyright (C) 2026 jmh
# SPDX-License-Identifier: GPL-3.0-only

import csv
import functools
import hashlib
import io
import os

from typing import TypedDict, get_type_hints, Optional

import bs4
import cairosvg

from bs4 import BeautifulSoup
from curl_cffi import requests
from PIL import Image


class MetadataEntry(TypedDict):
    icon: str
    url: Optional[str]
    name: Optional[str]
    image_url: Optional[str]
    image_hash: Optional[str]


type MetadataMap = dict[str, MetadataEntry]

REQUEST_TIMEOUT = 10

SVG_RENDER_SIZE = 256

SESSION = requests.Session(impersonate="chrome")

CURRENT_DIR = os.path.dirname(os.path.realpath(__file__))
MAIN_DIR = os.path.realpath(os.path.join(CURRENT_DIR, os.pardir))


def _empty_string_to_none(string: str) -> Optional[str]:
    return string if string != "" else None


def _get_current_metadata() -> MetadataMap:
    metafile_path = os.path.join(MAIN_DIR, "icons.csv")

    if not os.path.exists(metafile_path):
        return {}

    result = {}

    with open(metafile_path, "r", encoding="utf-8") as file:
        reader = csv.DictReader(file)

        for entry in reader:
            key = entry["icon"]

            result[key] = {
                "icon": entry["icon"],
                "url": _empty_string_to_none(entry["url"]),
                "name": _empty_string_to_none(entry["name"]),
                "image_url": _empty_string_to_none(entry["image_url"]),
                "image_hash": _empty_string_to_none(entry["image_hash"]),
            }

    return result


def _write_metadata(metadata: MetadataMap):
    metafile_path = os.path.join(MAIN_DIR, "icons.csv")

    with open(metafile_path, "w", encoding="utf-8") as file:
        writer = csv.DictWriter(
            file,
            fieldnames=get_type_hints(MetadataEntry).keys(),
        )

        writer.writeheader()

        for icon in sorted(metadata.keys()):
            writer.writerow(dict(metadata[icon]))


def _get_soup(url: str) -> bs4.BeautifulSoup:
    res = SESSION.get(url, timeout=REQUEST_TIMEOUT)
    res.raise_for_status()
    return BeautifulSoup(res.text, "html.parser")


def _get_favicon_from_soup(base_url: str, soup: bs4.BeautifulSoup) -> Optional[str]:
    link_tags = soup.find_all("link")

    for tag in link_tags:
        if tag.has_attr("rel") and any(
            rel in {"icon", "apple-touch-icon"} for rel in tag.get("rel")
        ):
            href = tag.get("href")

            if href.startswith("data:"):
                continue

            if href.startswith("http"):
                return href

            if href.startswith("www."):
                return f"https://{href}"

            if href.startswith("//"):
                return f"https:{href}"

            return base_url.rstrip("/") + "/" + href.lstrip("/")

    return None


def _strip_query_string(url: Optional[str]) -> Optional[str]:
    if url is None:
        return None

    query_string_pos = url.rfind("?")

    if query_string_pos == -1:
        return url

    return url[:query_string_pos]


def _decode_image(data: bytes, is_svg: bool) -> Image.Image:
    if is_svg:
        data = cairosvg.svg2png(bytestring=data, output_width=SVG_RENDER_SIZE)

    return Image.open(io.BytesIO(data)).convert("RGBA")


@functools.lru_cache(maxsize=None)
def _get_image_hash(image_url: str) -> Optional[str]:
    res = SESSION.get(image_url, timeout=REQUEST_TIMEOUT)

    if res.status_code == 404:
        return None

    res.raise_for_status()

    content_type = res.headers.get("content-type", "")

    if content_type.startswith("text/"):
        return None

    is_svg = content_type.startswith("image/svg") or image_url.endswith(".svg")

    try:
        image = _decode_image(res.content, is_svg)
    except Exception:
        return None

    return hashlib.md5(image.tobytes()).hexdigest()


def _enrich_metadata_entry(entry: MetadataEntry) -> MetadataEntry:
    if entry["url"] is not None:
        entry["url"] = entry["url"].rstrip("/")

        if entry["name"] is None or entry["image_url"] is None:
            print(
                "Getting icon '"
                + entry["icon"]
                + "' data from URL '"
                + entry["url"]
                + "'"
            )

            soup = _get_soup(entry["url"])

            if entry["name"] is None:
                entry["name"] = (
                    soup.title.text.replace("\n", "").strip()
                    if soup.title is not None
                    else None
                )

            if entry["image_url"] is None:
                entry["image_url"] = _strip_query_string(
                    _get_favicon_from_soup(entry["url"], soup)
                )
                entry["image_hash"] = None

    if entry["image_url"] is not None and entry["image_hash"] is None:
        entry["image_hash"] = _get_image_hash(entry["image_url"])

    return entry


def _has_icon_changed(entry: MetadataEntry) -> Optional[bool]:
    if entry["image_url"] is None or entry["image_hash"] is None:
        return False

    print(
        "Checking icon '"
        + entry["icon"]
        + "' at URL '"
        + entry["image_url"]
        + "' for changes"
    )

    latest_hash = _get_image_hash(entry["image_url"])
    return entry["image_hash"] != latest_hash


def _get_icon_names() -> list[str]:
    icons_dir = os.path.join(MAIN_DIR, "icons")
    return sorted(
        icon[:-4]
        for icon in os.listdir(icons_dir)
        if not icon.endswith("_dark.png") and icon != "default.png"
    )


def main():
    current_metadata = _get_current_metadata()
    new_metadata = {}

    icons = _get_icon_names()

    changed_icons = []
    not_found_icons = []

    for icon in icons:
        if icon not in current_metadata:
            entry: MetadataEntry = {
                "icon": icon,
                "url": None,
                "name": None,
                "image_url": None,
                "image_hash": None,
            }
        else:
            try:
                entry = _enrich_metadata_entry(current_metadata[icon])
            except Exception as e:
                print(f"error: Failed to get metadata: {e}")
                entry = current_metadata[icon]

        try:
            has_changed = _has_icon_changed(entry)

            if has_changed is None:
                not_found_icons.append(icon)
            elif has_changed:
                changed_icons.append(icon)
        except Exception as e:
            print(f"error: Failed to check for icon changes: {e}")

        new_metadata[icon] = entry

    _write_metadata(new_metadata)

    for icon in changed_icons:
        print(f"Icon '{icon}' hash changed since last check")

    for icon in not_found_icons:
        print(f"Icon '{icon}' not found, file probably moved")


if __name__ == "__main__":
    main()
