# /// script
# requires-python = ">=3.11"
# dependencies = ["requests"]
# ///
"""Fetch each person's Wikipedia lead image + its Commons license. Only files whose license is
public domain or CC are saved; everything is printed so the choice can be checked."""
import sys, requests
from urllib.parse import urlparse
from pathlib import Path
UA = {"User-Agent": "statecraft-israel/0.1 (https://github.com/eyalgolan/statecraft-israel)"}
OUT = Path(sys.argv[1])
PEOPLE = sys.argv[2:]
for title in PEOPLE:
    j = requests.get("https://en.wikipedia.org/w/api.php", headers=UA, timeout=30, params={
        "action": "query", "titles": title, "prop": "pageimages", "piprop": "original|name",
        "format": "json", "redirects": 1}).json()
    page = next(iter(j["query"]["pages"].values()))
    name, url = page.get("pageimage"), page.get("original", {}).get("source")
    if not url:
        print(f"NO IMAGE  {title}"); continue
    m = requests.get("https://commons.wikimedia.org/w/api.php", headers=UA, timeout=30, params={
        "action": "query", "titles": f"File:{name}", "prop": "imageinfo", "iiprop": "extmetadata",
        "format": "json"}).json()
    meta = next(iter(m["query"]["pages"].values())).get("imageinfo", [{}])[0].get("extmetadata", {})
    lic = meta.get("LicenseShortName", {}).get("value", "UNKNOWN")
    ok = lic.lower().startswith(("public domain", "pd", "cc"))
    if ok:
        r = requests.get(url, headers=UA, timeout=60); r.raise_for_status()
        (OUT / (title.replace(" ", "_") + Path(urlparse(url).path).suffix.lower())).write_bytes(r.content)
    print(f"{'SAVED' if ok else 'SKIP '}  {title:28} {lic:30} {name}")
