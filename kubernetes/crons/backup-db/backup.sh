#!/bin/bash

set -e

BACKUP_DIR="/backups"
BACKUP_FILE="backup_$(date +%Y%m%d%H%M%S).sql.gz"

# Create the backup directory if it doesn't exist
mkdir -p "${BACKUP_DIR}"

# Dump the database and compress the output
PGPASSWORD="${SQL_DB_PASSWORD}" pg_dump -U "${SQL_DB_USER}" -h "${SQL_DB_HOST}" -d "${SQL_DB_NAME}" -p "${SQL_DB_PORT}" | gzip > "${BACKUP_DIR}/${BACKUP_FILE}"

# Remove old backups, keeping only the last RETAIN_COUNT
cd "${BACKUP_DIR}"
ls -tp | grep -v '/$' | tail -n +$((RETAIN_COUNT + 1)) | xargs -I {} rm -- {}

echo "Backup completed: ${BACKUP_FILE}"
