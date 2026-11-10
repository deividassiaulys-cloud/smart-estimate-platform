"""
Loads the trial catalog through the existing smart-estimate-platform API (no code changes).

  1. Run the API (dotnet run) and register a test company in the UI.
  2. python seed.py --email manager@test.lt --password Password123 [--url https://localhost:7264]

Uses only POST /api/auth/login, GET/POST /api/materials and POST /api/works.
Existing materials / works (409) are skipped, so it is safe to run more than once.
"""
import argparse, json, os, ssl, urllib.request, urllib.error

DIR = os.path.dirname(os.path.abspath(__file__))
# the localhost dev certificate is often untrusted - local testing only
CTX = ssl._create_unverified_context()


def call(url, method, body=None, token=None):
    req = urllib.request.Request(url, method=method,
                                 data=json.dumps(body).encode() if body is not None else None,
                                 headers={"Content-Type": "application/json",
                                          **({"Authorization": f"Bearer {token}"} if token else {})})
    try:
        with urllib.request.urlopen(req, context=CTX) as r:
            return r.status, json.loads(r.read() or b"null")
    except urllib.error.HTTPError as e:
        return e.code, (e.read() or b"").decode("utf-8", "replace")


def main():
    a = argparse.ArgumentParser()
    a.add_argument("--url", default="https://localhost:7264")
    a.add_argument("--email", required=True)
    a.add_argument("--password", required=True)
    o = a.parse_args()

    st, auth = call(f"{o.url}/api/auth/login", "POST", {"email": o.email, "password": o.password})
    if st != 200:
        raise SystemExit(f"Login failed ({st}): {auth}")
    token = auth["token"]

    # existing materials (so a second run can find their ids)
    st, existing = call(f"{o.url}/api/materials", "GET", token=token)
    ids = {(m["name"], m["unit"]): m["id"] for m in (existing if st == 200 else [])}

    mats = json.load(open(f"{DIR}/materials.json", encoding="utf-8"))
    added = 0
    for m in mats:
        if (m["name"], m["unit"]) in ids:
            continue
        st, r = call(f"{o.url}/api/materials", "POST", m, token)
        if st in (200, 201):
            ids[(m["name"], m["unit"])] = r["id"]
            added += 1
        else:
            print(f"  material not added ({st}): {m['name']} – {r}")
    print(f"Materials: added {added}, in catalog {len(ids)}")

    works = json.load(open(f"{DIR}/works.json", encoding="utf-8"))
    added = skipped = 0
    for w in works:
        body = {"name": w["name"], "unit": w["unit"], "labourRate": w["labourRate"],
                "labourNorm": w["labourNorm"],
                "lines": [{"materialId": ids[(l["material"], l["materialUnit"])],
                           "consumptionNorm": l["consumptionNorm"], "wastePercent": l["wastePercent"]}
                          for l in w["lines"]]}
        st, r = call(f"{o.url}/api/works", "POST", body, token)
        if st in (200, 201):
            added += 1
        elif st == 409:
            skipped += 1
        else:
            print(f"  work not added ({st}): {w['name']} – {r}")
    print(f"Works: added {added}, already existed {skipped}")


if __name__ == "__main__":
    main()
