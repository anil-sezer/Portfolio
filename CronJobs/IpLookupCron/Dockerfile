FROM golang:1.21 as builder

WORKDIR /app

COPY . .

RUN CGO_ENABLED=0 GOOS=linux go build -a -installsuffix cgo -o IpLookupCron .

FROM alpine:latest
RUN apk --no-cache add ca-certificates

WORKDIR /root/

COPY --from=builder /app/IpLookupCron .

CMD ["./IpLookupCron"]
