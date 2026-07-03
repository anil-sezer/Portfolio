# docker buildx build -t imgregistry.anil-sezer.com/pg_dump:v18 --platform linux/amd64,linux/arm64 --push .
FROM postgres:18.1

# Install apprise
RUN apt-get update && apt-get install -y apprise && rm -rf /var/lib/apt/lists/*

# Set up the backup script
COPY backup.sh common.sh /usr/local/bin/
RUN chmod +x /usr/local/bin/backup.sh /usr/local/bin/common.sh

ENV BACKUP_DIR=/backups

# Create backup directory
RUN mkdir -p "${BACKUP_DIR}" && chown postgres:postgres "${BACKUP_DIR}"

VOLUME ["${BACKUP_DIR}"]

# Set the script as the container's entrypoint
ENTRYPOINT ["/usr/local/bin/backup.sh"]
