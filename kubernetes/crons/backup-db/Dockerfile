# docker buildx build -t anilsezer/pg_dump:latest --platform linux/amd64,linux/arm64 . && docker push anilsezer/pg_dump:latest
FROM postgres:16.2-bullseye

# Set up the backup script
COPY backup.sh /usr/local/bin/backup.sh
RUN chmod +x /usr/local/bin/backup.sh

ENV BACKUP_DIR=/backups

# Create backup directory
RUN mkdir -p "${BACKUP_DIR}" && chown postgres:postgres "${BACKUP_DIR}"

VOLUME ["${BACKUP_DIR}"]

# Set the script as the container's entrypoint
ENTRYPOINT ["/usr/local/bin/backup.sh"]
