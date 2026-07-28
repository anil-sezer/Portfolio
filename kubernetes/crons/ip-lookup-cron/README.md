# This cron is used to determine the countries of visitors to the website.

# Build & push:
docker buildx build -t imgregistry.anil-sezer.com/iplookup-cron-go:latest --platform linux/amd64,linux/arm64 --push .