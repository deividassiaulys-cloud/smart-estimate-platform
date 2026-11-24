#!/usr/bin/env bash
# Smoke test of a deployed Smart Estimate Platform (feature 022, T372). It checks the live client, API and database
# together, in 8 steps, and stops at the first failure with a non-zero exit code:
#   1. /health is Healthy (and reports EXPECTED_VERSION when given)
#   2. /health/ready is Healthy (the database answers)
#   3. the client's start page is served
#   4. a throwaway company A registers and logs in
#   5. A creates a material (201)
#   6. A reads it back by id in a new request: same name, unit and price
#   7. a throwaway company B gets 404 for A's material
#   8. a request without a token gets 401
#
# Usage:
#   BASE_URL=https://<SITE_ADDRESS> [EXPECTED_VERSION=<commit sha>] [SMOKE_CA_CERT=<root.crt>] tests/smoke/smoke-test.sh
# SMOKE_CA_CERT is only for a local run against Caddy's own certificate authority (never -k).
#
# It prints no token or password. The passwords are random for every run, and the emails use the reserved .invalid
# domain. The two companies stay in the database: there is no endpoint to delete a company (docs/deployment.md has
# the cleanup SQL). It sends 2 registrations and 2 logins, under the limit of 10 per minute.
set -euo pipefail

: "${BASE_URL:?Set BASE_URL, e.g. https://203.0.113.10.sslip.io}"
BASE_URL="${BASE_URL%/}"
EXPECTED_VERSION="${EXPECTED_VERSION:-}"

for tool in curl jq openssl; do
  command -v "${tool}" >/dev/null || { echo "FAIL 0 setup: ${tool} is not installed" >&2; exit 1; }
done

work="$(mktemp -d)"
trap 'rm -rf "${work}"' EXIT
chmod 700 "${work}"

curl_options=(--silent --show-error --max-time 20)
if [[ -n "${SMOKE_CA_CERT:-}" ]]; then
  curl_options+=(--cacert "${SMOKE_CA_CERT}")
fi

step=0
name=""
begin() { step=$((step + 1)); name="$1"; }
ok() { echo "ok ${step} ${name}"; }
fail() { echo "FAIL ${step} ${name}: $1" >&2; exit 1; }

# request METHOD PATH [AUTH_FILE] [JSON_BODY]: sets STATUS; the body is in ${work}/body.
# A token is read by curl from a header file (mode 600), so it never appears on a command line.
request() {
  local method="$1" path="$2" auth="${3:-}" body="${4:-}"
  local args=(--request "${method}" --output "${work}/body" --write-out '%{http_code}')
  if [[ -n "${auth}" ]]; then
    args+=(--header "@${auth}")
  fi
  if [[ -n "${body}" ]]; then
    args+=(--header 'Content-Type: application/json' --data-binary "${body}")
  fi
  STATUS="$(curl "${curl_options[@]}" "${args[@]}" "${BASE_URL}${path}" 2>"${work}/curl-error")" || STATUS="000"
  if [[ "${STATUS}" == "000" ]]; then
    STATUS="000 ($(head -c 200 "${work}/curl-error"))"
  fi
}

expect_status() {
  [[ "${STATUS}" == "$1" ]] || fail "expected HTTP $1, got ${STATUS}"
}

json() { jq -er "$1" "${work}/body" 2>/dev/null; }

# register_and_login LETTER: registers company LETTER and logs in; writes its Authorization header to a file.
run_id="$(date -u +%Y%m%dT%H%M%SZ)-$(openssl rand -hex 3)"
register_and_login() {
  local letter="$1" email password registration login
  email="smoke-${run_id}-${letter}@smoke.invalid"
  password="$(openssl rand -hex 16)"
  registration="$(jq -nc --arg company "Smoke ${run_id} ${letter}" --arg email "${email}" --arg password "${password}" \
    '{companyName: $company, firstName: "Smoke", lastName: "Test", email: $email, password: $password}')"
  request POST /api/auth/register "" "${registration}"
  expect_status 201

  login="$(jq -nc --arg email "${email}" --arg password "${password}" '{email: $email, password: $password}')"
  request POST /api/auth/login "" "${login}"
  expect_status 200
  local token
  token="$(json '.token')" || fail "the login response has no token"
  (umask 077 && printf 'Authorization: Bearer %s\n' "${token}" > "${work}/auth-${letter}")
}

begin "health"
request GET /health
expect_status 200
[[ "$(json '.status')" == "Healthy" ]] || fail "expected status Healthy, got $(head -c 200 "${work}/body")"
version="$(json '.version')" || fail "the response has no version"
if [[ -n "${EXPECTED_VERSION}" && "${version}" != "${EXPECTED_VERSION}" ]]; then
  fail "expected version ${EXPECTED_VERSION}, got ${version}"
fi
ok

begin "ready"
request GET /health/ready
expect_status 200
[[ "$(cat "${work}/body")" == "Healthy" ]] || fail "expected Healthy, got $(head -c 200 "${work}/body")"
ok

begin "client start page"
request GET /
expect_status 200
grep -q 'id="root"' "${work}/body" || fail "the start page has no React root element"
ok

begin "company A registers and logs in"
register_and_login a
ok

begin "A creates a material"
material_name="Smoke material ${run_id}"
material="$(jq -nc --arg name "${material_name}" '{name: $name, unit: "m²", price: 12.3456}')"
request POST /api/materials "${work}/auth-a" "${material}"
expect_status 201
material_id="$(json '.id')" || fail "the response has no id"
ok

begin "A reads the material back"
request GET "/api/materials/${material_id}" "${work}/auth-a"
expect_status 200
jq -e --arg name "${material_name}" '.name == $name and .unit == "m²" and .price == 12.3456' "${work}/body" >/dev/null \
  || fail "expected ${material_name}, m², 12.3456; got $(jq -c '{name, unit, price}' "${work}/body" 2>/dev/null)"
ok

begin "company B gets 404 for A's material"
register_and_login b
request GET "/api/materials/${material_id}" "${work}/auth-b"
expect_status 404
ok

begin "anonymous request gets 401"
request GET /api/materials
expect_status 401
ok

echo "Smoke test passed: ${BASE_URL} (version ${version})"
