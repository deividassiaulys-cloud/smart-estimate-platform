#!/usr/bin/env bash
# Deploys one image tag on this server (feature 022, T370). Run it from anywhere; it works in its own directory,
# next to docker-compose.yml, Caddyfile and .env:
#   IMAGE_TAG=<commit sha> ./deploy.sh
# Steps:
#   1. pull the images;
#   2. start MySQL;
#   3. back up the database;
#   4. apply the migrations;
#   5. start the new API and Caddy;
#   6. wait until the new API is ready;
#   7. record the tag.
# It stops at the first failure. The running API is replaced only after the migrations succeeded.
#
# Options (environment variables):
#   PULL=0          use local images instead of pulling (for a local run)
#   BACKUP_DIR=…    where dumps go (default /var/backups/smart-estimate); the newest KEEP_BACKUPS (7) are kept
#   SITE_ADDRESS=…  taken from .env when not set
#   DEPLOYED_TAG_FILE=…  where the deployed tag is recorded (default .deployed-tag next to this script)
#
# Rollback: run it again with the previous tag, which it prints. Migrations are forward-only: an older bundle
# does not undo newer migrations. To undo a schema change, restore the dump (docs/deployment.md).
set -euo pipefail

cd "$(dirname "$0")"

: "${IMAGE_TAG:?Set IMAGE_TAG to the commit SHA to deploy}"
export IMAGE_TAG
PULL="${PULL:-1}"
BACKUP_DIR="${BACKUP_DIR:-/var/backups/smart-estimate}"
KEEP_BACKUPS="${KEEP_BACKUPS:-7}"
DEPLOYED_TAG_FILE="${DEPLOYED_TAG_FILE:-.deployed-tag}"
if [[ -z "${SITE_ADDRESS:-}" && -f .env ]]; then
  SITE_ADDRESS="$(grep -E '^SITE_ADDRESS=' .env | tail -n 1 | cut -d= -f2-)"
fi
: "${SITE_ADDRESS:?Set SITE_ADDRESS in .env}"

compose() { docker compose --profile migrate "$@"; }
step() { printf '\n== %s\n' "$*"; }

previous_tag="$(cat "${DEPLOYED_TAG_FILE}" 2>/dev/null || echo none)"
echo "Deploying ${IMAGE_TAG} to ${SITE_ADDRESS} (previous: ${previous_tag})"

if [[ "${PULL}" != "0" ]]; then
  step "Pulling images"
  compose pull --quiet
fi

step "Starting MySQL"
docker compose up -d --wait mysql

step "Backing up the database"
mkdir -p "${BACKUP_DIR}"
backup="${BACKUP_DIR}/$(date -u +%Y%m%dT%H%M%SZ)-${IMAGE_TAG}.sql.gz"
# The credentials stay inside the container (its own environment); a dump that fails half-way is not kept.
docker compose exec -T mysql sh -c \
  'MYSQL_PWD="$MYSQL_PASSWORD" mysqldump --single-transaction --no-tablespaces -u"$MYSQL_USER" "$MYSQL_DATABASE"' \
  | gzip > "${backup}.partial"
mv "${backup}.partial" "${backup}"
chmod 600 "${backup}"
echo "Backup: ${backup}"
# The names are this script's own (<UTC time>-<tag>.sql.gz), so sorting ls output by time is safe here.
# shellcheck disable=SC2012
ls -1t "${BACKUP_DIR}"/*.sql.gz | tail -n +"$((KEEP_BACKUPS + 1))" | while read -r old; do rm -f -- "${old}"; done

step "Applying migrations"
if ! compose run --rm migrate; then
  echo "Migration failed; API not restarted; backup: ${backup}" >&2
  echo "The previous version (${previous_tag}) keeps running." >&2
  exit 1
fi

step "Starting the new API and Caddy"
docker compose up -d api caddy

step "Waiting for the new API"
# Asked from inside the Docker network, through the Caddy container, so neither DNS nor the certificate matters
# here. The smoke test checks the public address. The Host header must be one the API allows.
health() { docker compose exec -T caddy wget -q -O - --header "Host: ${SITE_ADDRESS}" "http://api:8080$1" 2>/dev/null; }
for _ in $(seq 1 30); do
  if [[ "$(health /health/ready || true)" == "Healthy" ]] && health /health | grep -q "\"version\":\"${IMAGE_TAG}\""; then
    echo "${IMAGE_TAG}" > "${DEPLOYED_TAG_FILE}"
    echo "Deployed ${IMAGE_TAG} (previous: ${previous_tag}); backup: ${backup}"
    exit 0
  fi
  sleep 2
done

echo "The new API did not become ready with version ${IMAGE_TAG} within 60 seconds." >&2
echo "Logs: docker compose logs api --since 5m. Roll back: IMAGE_TAG=${previous_tag} ./deploy.sh" >&2
exit 1
