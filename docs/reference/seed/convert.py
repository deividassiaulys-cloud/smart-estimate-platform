"""
Converts docs/reference/sample-estimates (resources.json + rate-templates.json) into the
smart-estimate-platform API request format (MaterialRequest / WorkRequest) and checks how far the
sample estimate would be from the PDF when calculated with the CURRENT rules (BR-015 to BR-017),
without any code changes.

  python convert.py ../sample-estimates
"""
import json, sys, os
from decimal import Decimal, ROUND_HALF_UP
from collections import OrderedDict

SRC = sys.argv[1] if len(sys.argv) > 1 else "../sample-estimates"
OUT = os.path.dirname(os.path.abspath(__file__))

# MeasurementUnit.All = vnt., m, m², m³, kg, t, l, val., kompl.
UNIT = {"m2": "m²", "m3": "m³", "m": "m", "vnt": "vnt.", "vnt.": "vnt.", "ltr": "l",
        "žm.val.": "val.", "žm.val": "val.", "val.": "val.", "kompl.": "kompl."}
# units missing from the fixed list are moved into the name
UNIT_FALLBACK = {"tub.": ("vnt.", "tūb."), "ind.": ("vnt.", "ind."), "rul.": ("vnt.", "rul."),
                 "pak.": ("kompl.", "pak."), "reisas": ("kompl.", "reisas"),
                 "km": ("kompl.", "1 km"), "d.d.": ("kompl.", "d.d.")}


def money(x):
    return Decimal(str(x)).quantize(Decimal("0.01"), rounding=ROUND_HALF_UP)


def unit_of(u):
    if u in UNIT:
        return UNIT[u], None
    return UNIT_FALLBACK[u]


res = {r["kodas"]: r for r in (
    {"kodas": x["code"], "pavadinimas": x["name"], "mato_vnt": x["unit"], "kaina": x["price"],
     "tipas": {"labour": "darbas", "material": "medziaga", "machinery": "mechanizmas"}[x["type"]]}
    for x in json.load(open(f"{SRC}/resources.json", encoding="utf-8"))["resources"])}
def _lines(ls):
    return [{"kodas": l["code"], "norma": l["norm"], **({"kaina": l["price"]} if "price" in l else {})} for l in ls]


tpls = [{"kodas": t["code"], "pavadinimas": t["name"], "mato_vnt": t["unit"], "kategorija": t["category"],
         "resursai": _lines(t["resources"])}
        for t in json.load(open(f"{SRC}/rate-templates.json", encoding="utf-8"))["rateTemplates"]]
_p = json.load(open(f"{SRC}/sample-estimate.json", encoding="utf-8"))
project = {"skyriai": [{"eilutes": [{"pavadinimas": l["name"], "mato_vnt": l["unit"], "kiekis": l["quantity"],
                                     "resursai": _lines(l["resources"])} for l in s["lines"]]}
                       for s in _p["sections"]]}

materials = OrderedDict()   # (name, unit) -> {name, unit, price}
report = []


def material_for(code, price):
    """Returns (material key, norm divisor). The price must fit DECIMAL(18,2)."""
    r = res[code]
    unit, note = unit_of(r["mato_vnt"])
    name = r["pavadinimas"] + (f" ({note})" if note else "")
    if r["tipas"] == "mechanizmas":
        name += " [mech.]"
    factor = 1
    p = Decimal(str(price))
    if p != money(p):  # sub-cent price -> pack of 10/100/1000 units
        for k in (10, 100, 1000):
            if abs(money(p * k) - p * k) <= p * k * Decimal("0.005"):
                factor = k
                break
        else:
            factor = 1000
        name += f" (pack {factor} {unit})"
        unit = "kompl."
    price2 = money(p * factor)
    key = (name, unit)
    if key in materials and materials[key]["price"] != price2:
        key = (f"{name} – {price2} €", unit)
    materials.setdefault(key, {"name": key[0], "unit": unit, "price": price2})
    return key, factor


works = []
names_seen = {}
for t in tpls:
    labour = [(l, res[l["kodas"]]) for l in t["resursai"] if res[l["kodas"]]["tipas"] == "darbas"]
    labour_cost_per_unit = sum(Decimal(str(l["norma"])) * Decimal(str(l.get("kaina", r["kaina"])))
                               for l, r in labour)
    rate = Decimal(str(labour[0][1]["kaina"] if "kaina" not in labour[0][0] else labour[0][0]["kaina"])) if labour else Decimal(0)
    norm = (labour_cost_per_unit / rate).quantize(Decimal("0.001"), rounding=ROUND_HALF_UP) if rate else Decimal(0)
    lines = OrderedDict()
    for l in t["resursai"]:
        r = res[l["kodas"]]
        if r["tipas"] == "darbas":
            continue
        key, factor = material_for(l["kodas"], l.get("kaina", r["kaina"]))
        lines[key] = lines.get(key, Decimal(0)) + Decimal(str(l["norma"])) / factor
    out_lines, dropped = [], []
    for key, n in lines.items():
        n3 = n.quantize(Decimal("0.001"), rounding=ROUND_HALF_UP)
        if n3 <= 0:
            dropped.append(f"{key[0]} (norm {n:.6f} < 0.0005)")
            continue
        out_lines.append({"material": key[0], "materialUnit": key[1], "consumptionNorm": float(n3),
                          "wastePercent": 0})
    unit, note = unit_of(t["mato_vnt"])
    name = t["pavadinimas"] + (f" ({note})" if note else "")
    k = (name, unit)
    if k in names_seen:  # BR-006: name + unit must be unique
        name = f"{name} – {t['kategorija'][:60]}"
        if (name, unit) in names_seen:
            name = f"{name} ({t['kodas']})"
    names_seen[(name, unit)] = t["kodas"]
    works.append({"source": t["kodas"], "name": name[:200], "unit": unit,
                  "labourRate": float(rate), "labourNorm": float(norm), "lines": out_lines})
    if dropped:
        report.append(f"{t['kodas']} {t['pavadinimas']}: dropped {', '.join(dropped)}")
    if len({float(l.get('kaina', r['kaina'])) for l, r in labour}) > 1:
        report.append(f"{t['kodas']} {t['pavadinimas']}: several labour rates merged into {rate} EUR/h")

# ---- check: sample estimate with the current BR-015..BR-017 ----
by_source = {w["source"]: w for w in works}
mat_price = {(m["name"], m["unit"]): m["price"] for m in materials.values()}
tpl_by_sig = {}
for t in tpls:
    tpl_by_sig[(t["pavadinimas"], t["mato_vnt"],
                tuple((l["kodas"], l["norma"], l.get("kaina")) for l in t["resursai"]))] = t["kodas"]
direct = Decimal(0)
for s in project["skyriai"]:
    for it in s["eilutes"]:
        sig = (it["pavadinimas"], it["mato_vnt"],
               tuple((l["kodas"], l["norma"], l.get("kaina")) for l in it["resursai"]))
        w = by_source[tpl_by_sig[sig]]
        q = Decimal(str(it["kiekis"]))
        if q == 0:
            continue
        direct += money(q * Decimal(str(w["labourNorm"])) * Decimal(str(w["labourRate"])))
        for l in w["lines"]:
            direct += money(q * Decimal(str(l["consumptionNorm"])) * mat_price[(l["material"], l["materialUnit"])])
overhead = money(direct * Decimal("0.015"))
profit = money((direct + overhead) * Decimal("0.06"))
vat = money((direct + overhead + profit) * Decimal("0.21"))
total = direct + overhead + profit + vat

json.dump([{"name": m["name"], "unit": m["unit"], "price": float(m["price"])} for m in materials.values()],
          open(f"{OUT}/materials.json", "w", encoding="utf-8"), ensure_ascii=False, indent=1)
json.dump(works, open(f"{OUT}/works.json", "w", encoding="utf-8"), ensure_ascii=False, indent=1)
with open(f"{OUT}/conversion-report.txt", "w", encoding="utf-8") as f:
    f.write(f"Materials: {len(materials)}, works: {len(works)}\n\n")
    f.write("Sample estimate with the smart-estimate-platform calculation (overhead 1.5 %, profit 6 %, VAT 21 %):\n")
    f.write(f"  Direct costs {direct}  (PDF 104017.69)\n  Overhead {overhead}\n  Profit {profit}\n"
            f"  VAT {vat}\n  Total {total}  (PDF 135414.29, difference {total - Decimal('135414.29'):+})\n\n")
    f.write("Notes:\n" + "\n".join("  " + x for x in report) + "\n")
print(open(f"{OUT}/conversion-report.txt", encoding="utf-8").read()[:1500])
