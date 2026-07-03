#!/bin/bash

# With set -e, any failure stops the script right there, preventing partial or corrupt backups.
set -e
set -o pipefail

source common.sh

success=false

cleanup() {
  local exit_code=$?
  if [ "$success" = "true" ]; then
    colored_echo "$GREEN" "Sending success notification to Telegram..."
    send_telegram_notification "ENV: ${ENV_NAME} - Database backup completed for this database: ${SQL_DB_NAME}. File: ${BACKUP_FILE}" "success"
  else
    colored_echo "$RED" "Sending failure notification to Telegram..."
    if [ -n "${BACKUP_FILE}" ]; then
      send_telegram_notification "ENV: ${ENV_NAME} - Database backup failed for this database: ${SQL_DB_NAME}. File: ${BACKUP_FILE}" "error"
    else
      send_telegram_notification "ENV: ${ENV_NAME} - Database backup failed for this database: ${SQL_DB_NAME} during setup (exit code: ${exit_code})." "error"
    fi
  fi
}
trap cleanup EXIT

print_header

colored_echo "$BLUE" "Backup started at: $(date)"
  
# Check for required environment variables
for var in SQL_DB_PASSWORD SQL_DB_USER SQL_DB_HOST SQL_DB_NAME SQL_DB_PORT RETAIN_COUNT; do
  if [ -z "${!var}" ]; then
    colored_echo "$RED" "ERROR: Required variable $var is not set!"
    exit 1
  fi
done

# Check if database connection is successful
if ! pg_isready -h "${SQL_DB_HOST}" -p "${SQL_DB_PORT}" -U "${SQL_DB_USER}" -d "${SQL_DB_NAME}" >/dev/null 2>&1; then
  colored_echo "$RED" "ERROR: Failed to connect to the database."
  exit 1
fi

BACKUP_DIR="${BACKUP_DIR:-/backups}"
BACKUP_FILE="backup_$(date +%Y%m%d%H%M%S).sql.gz"

# Create the backup directory if it doesn't exist
mkdir -p "${BACKUP_DIR}"

# Dump the database and compress the output
PGPASSWORD="${SQL_DB_PASSWORD}" pg_dump -U "${SQL_DB_USER}" -h "${SQL_DB_HOST}" -d "${SQL_DB_NAME}" -p "${SQL_DB_PORT}" | gzip > "${BACKUP_DIR}/${BACKUP_FILE}"

if [ ! -f "${BACKUP_DIR}/${BACKUP_FILE}" ] || [ ! -s "${BACKUP_DIR}/${BACKUP_FILE}" ]; then
  colored_echo "$RED" "ERROR: Backup file was not created or is empty!"
  exit 1
fi

# Remove old backups, keeping only the last RETAIN_COUNT
cd "${BACKUP_DIR}"
ls -tp | grep -v '/$' | tail -n +$((RETAIN_COUNT + 1)) | xargs -I {} rm -- {}

colored_echo "$GREEN" "Backup completed for this database: ${SQL_DB_NAME}. ENV: ${ENV_NAME}. File: ${BACKUP_FILE}"
success=true
