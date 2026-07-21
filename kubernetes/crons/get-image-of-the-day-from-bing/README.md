# Build & push:
docker buildx build -t imgregistry.anil-sezer.com/get-daily-images-go-cron:latest --platform linux/amd64,linux/arm64 --push .