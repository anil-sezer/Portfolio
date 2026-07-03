# Test locally
docker build -t imgregistry.anil-sezer.com/pg_dump:v18 . ; docker compose run --rm --build postgres-backup

# Build:
docker buildx build -t imgregistry.anil-sezer.com/pg_dump:v18 --platform linux/amd64,linux/arm64 --push .

# Verify: 
docker compose run --rm --entrypoint "zcat" postgres-backup /backups/backup_20260705160513.sql.gz > test_dump.sql