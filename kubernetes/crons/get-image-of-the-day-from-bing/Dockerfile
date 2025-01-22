# docker build -t get-image-of-the-day-from-bing .
# docker build -f deployment/crons/get-image-of-the-day-from-bing/Dockerfile -t img-from-bing .
# docker run --name get-image-of-the-day-from-bing --env-file .env get-image-of-the-day-from-bing
FROM golang:1.23.4 AS builder
LABEL  maintainer="Anil Sezer - msnanil@hotmail.com"

WORKDIR /app

COPY deployment/crons/get-image-of-the-day-from-bing/ .
COPY protos/background_images/persist .

RUN CGO_ENABLED=0 GOOS=linux go build -a -installsuffix cgo -o get-image-of-the-day-from-bing .

FROM alpine:latest
RUN apk --no-cache add ca-certificates

WORKDIR /root/

COPY --from=builder /app/get-image-of-the-day-from-bing .

CMD ["./get-image-of-the-day-from-bing"]
