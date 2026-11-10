"""
Reference calculator for the sample estimates (the method used in the source PDFs).

  python reference_calculator.py          # print the totals block and write sample-estimate-export.csv
  python reference_calculator.py --check  # compare with the PDF amounts

Method (each amount is also split into labour / material / machinery columns):
  resource quantity = norm x line quantity
  resource amount   = round(quantity x price, 2)          (half away from zero)
  line amount       = sum of resource amounts; unit price = line amount / line quantity
  section / total   = sums of line amounts
  surcharges        = 1.5 % of material, 1.5 % of machinery, 1.5 % of labour (+ social insurance, site costs)
  direct costs -> + profit 6 % -> - discount -> + VAT 21 % (0 % in the second estimate)
"""
import csv, json, os, sys
from decimal import Decimal, ROUND_HALF_UP

DIR = os.path.dirname(os.path.abspath(__file__))
COLS = ("labour", "material", "machinery")


def r2(x):
    return float(Decimal(str(x)).quantize(Decimal("0.01"), rounding=ROUND_HALF_UP))


def load(name):
    with open(os.path.join(DIR, name), encoding="utf-8") as f:
        return json.load(f)


def settings(overrides=None):
    s = load("settings.json")
    s.update(overrides or {})
    return s


def calc_line(line, catalog):
    cols = dict.fromkeys(COLS, 0.0)
    rows = []
    for ln in line["resources"]:
        res = catalog[ln["code"]]
        price = ln.get("price", res["price"])
        qty = ln["norm"] * line["quantity"]
        amount = r2(qty * price)
        cols[res["type"]] += amount
        rows.append(dict(res=res, norm=ln["norm"], price=price, qty=qty, amount=amount))
    cols = {k: r2(v) for k, v in cols.items()}
    return r2(sum(cols.values())), cols, rows


def totals_block(direct, s):
    c = dict(direct)
    rows = [("Total", dict(c))]
    for p in s["surcharges"]:
        if p["percent"]:
            add = r2(c[p["column"]] * p["percent"] / 100)
            rows.append((f'{p["name"]} {p["percent"]}%', {p["column"]: add}))
            c[p["column"]] = r2(c[p["column"]] + add)
    rows.append(("Total (direct costs)", dict(c)))
    direct_total = r2(sum(c.values()))
    profit = r2(direct_total * s["profitPercent"] / 100)
    rows.append((f'Profit {s["profitPercent"]}%', {"amount": profit}))
    after_profit = r2(direct_total + profit)
    discount = r2(after_profit * s.get("discountPercent", 0) / 100)
    if discount:
        rows.append((f'Discount {s["discountPercent"]}%', {"amount": -discount}))
    indirect = r2(after_profit - discount)
    rows.append(("Total (incl. indirect costs)", {"amount": indirect}))
    vat = r2(indirect * s["vatPercent"] / 100)
    rows.append((f'VAT {s["vatPercent"]}%', {"amount": vat}))
    grand = r2(indirect + vat)
    rows.append(("Grand total", {"amount": grand}))
    return grand, rows


def calculate(estimate, catalog, s):
    total = dict.fromkeys(COLS, 0.0)
    sections = []
    for sec in estimate["sections"]:
        st = dict.fromkeys(COLS, 0.0)
        lines = []
        for line in sec["lines"]:
            amount, cols, rows = calc_line(line, catalog)
            for k in COLS:
                st[k] += cols[k]
            lines.append((line, amount, cols, rows))
        st = {k: r2(v) for k, v in st.items()}
        for k in COLS:
            total[k] += st[k]
        sections.append((sec, st, lines))
    total = {k: r2(v) for k, v in total.items()}
    grand, rows = totals_block(total, s)
    return sections, total, grand, rows


def fmt(x):
    return f"{x:,.2f}" if x else ""


def export_csv(sections, rows, path):
    with open(path, "w", newline="", encoding="utf-8-sig") as f:
        w = csv.writer(f)
        w.writerow(["No.", "Name", "Code", "Unit", "Norm", "Price", "Quantity", "Amount",
                    "Labour", "Materials", "Machinery"])
        for sec, st, lines in sections:
            w.writerow(["", "++" + sec["name"]])
            for line, amount, cols, res_rows in lines:
                unit_price = amount / line["quantity"] if line["quantity"] else 0
                w.writerow([line["no"], line["name"], "", line["unit"], f"{unit_price:.4f}", "",
                            f'{line["quantity"]:g}', fmt(amount),
                            fmt(cols["labour"]), fmt(cols["material"]), fmt(cols["machinery"])])
                for r in res_rows:
                    c = ["", "", ""]
                    c[COLS.index(r["res"]["type"])] = fmt(r["amount"])
                    w.writerow(["", r["res"]["name"], r["res"]["code"], r["res"]["unit"],
                                f'{r["norm"]:.3f}', f'{r["price"]:.4f}' if r["price"] < 1 else fmt(r["price"]),
                                f'{r["qty"]:.3f}', fmt(r["amount"]), *c])
            w.writerow(["", "Section total", "", "", "", "", "", fmt(sum(st.values())),
                        fmt(st["labour"]), fmt(st["material"]), fmt(st["machinery"])])
        for name, vals in rows:
            if "amount" in vals:
                w.writerow(["", name, "", "", "", "", "", fmt(vals["amount"])])
            else:
                w.writerow(["", name, "", "", "", "", "", fmt(sum(vals.values())),
                            fmt(vals.get("labour", 0)), fmt(vals.get("material", 0)),
                            fmt(vals.get("machinery", 0))])


def main():
    catalog = {x["code"]: x for x in load("resources.json")["resources"]}
    estimate = load("sample-estimate.json")
    s = settings(estimate.get("settings"))
    sections, total, grand, rows = calculate(estimate, catalog, s)

    if "--check" in sys.argv:
        bad = 0
        for sec, st, lines in sections:
            for line, amount, cols, _ in lines:
                d = amount - line["pdfAmount"]
                if abs(d) > 0.05:
                    bad += 1
                    print(f'  line {line["no"]:>3} {line["name"][:45]:45} calc {amount:10.2f} '
                          f'PDF {line["pdfAmount"]:10.2f} diff {d:+.2f}')
        print(f"Lines differing by more than 0.05 EUR: {bad}")
        print(f"Direct (before surcharges): {sum(total.values()):.2f}  (PDF 104017.69)")
        print(f"Grand total: {grand:.2f}  (PDF 135414.29)")
        g2, _ = totals_block({"labour": 189184.58, "material": 255516.79, "machinery": 10000.21},
                             settings({"vatPercent": 0}))
        print(f"Second estimate surcharge chain: {g2:.2f}  (PDF 489213.42)")
        return

    path = os.path.join(DIR, "sample-estimate-export.csv")
    export_csv(sections, rows, path)
    for name, vals in rows:
        print(f"{name:45} {fmt(vals['amount'] if 'amount' in vals else sum(vals.values())):>14}")
    print("CSV:", path)


if __name__ == "__main__":
    main()
