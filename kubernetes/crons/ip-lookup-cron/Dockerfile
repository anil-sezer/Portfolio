# docker build -t ip-lookup-cron .
# docker run --name ip-lookup-cron --env-file .env ip-lookup-cron
FROM golang:1.23.4 AS builder
LABEL  maintainer="Anil Sezer - msnanil@hotmail.com"

WORKDIR /app

COPY . .

RUN CGO_ENABLED=0 GOOS=linux go build -a -installsuffix cgo -o ip-lookup-cron .

FROM alpine:latest
RUN apk --no-cache add ca-certificates

WORKDIR /root/

COPY --from=builder /app/ip-lookup-cron .

CMD ["./ip-lookup-cron"]
