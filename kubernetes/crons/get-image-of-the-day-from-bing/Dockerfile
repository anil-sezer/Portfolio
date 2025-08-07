# docker build -t anilsezer/get-daily-images-go-cron:latest . && docker push anilsezer/get-daily-images-go-cron:latest
FROM golang:1.23.4 AS builder
LABEL  maintainer="Anil Sezer - msnanil@hotmail.com"

WORKDIR /app

COPY . .

RUN CGO_ENABLED=0 GOOS=linux go build -a -installsuffix cgo -o get-image-of-the-day-from-bing .

FROM alpine:latest
RUN apk --no-cache add ca-certificates

WORKDIR /root/

COPY --from=builder /app/get-image-of-the-day-from-bing .

CMD ["./get-image-of-the-day-from-bing"]
