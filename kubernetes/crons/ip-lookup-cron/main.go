package main

import (
	"errors"
	"fmt"
	"log"
	"strings"
	"time"

	"ip-lookup-cron/grpc"
	"ip-lookup-cron/proto"
	"ip-lookup-cron/third_party"

	"github.com/joho/godotenv"
)

// todo: Redo this main.go. It has too much stuff in it. 

const (
	lookupSuccessDelay            = 2 * time.Second
	lookupErrorDelay              = 10 * time.Second
	maxConsecutiveTransientErrors = 3
)

type cachedLookup struct {
	info      *third_party.IPInfo
	permanent bool
	transient bool
}

func main() {
	loadEnvValues()

	ips := grpc.GetIpsToCheckFromGrpc()
	if len(ips) == 0 {
		log.Println("No IPs to process. Exiting.")
		return
	}

	processIPs(ips)

	if hasProcessedRows(ips) {
		grpc.SendIpCheckResultsToGrpc(ips)
	} else {
		log.Println("No rows updated, deleted, or ignored; skipping gRPC persistence.")
	}

	logTheResult(ips)
}

func loadEnvValues() {
	if err := godotenv.Load(); err != nil {
		log.Println("Env file not present, using system defaults")
	}
}

func processIPs(ips []*proto.IpCheckDto) {
	processIPsWithDeps(ips, third_party.GetIPInfo, time.Sleep)
}

func processIPsWithDeps(
	ips []*proto.IpCheckDto,
	lookupFn func(string) (*third_party.IPInfo, error),
	sleepFn func(time.Duration),
) {
	ipCache := make(map[string]cachedLookup)
	consecutiveTransientErrors := 0
	circuitOpen := false

	for _, ip := range ips {
		if isPrivateOrIgnoredIP(ip.IpAddress) {
			log.Printf("Will delete private/ignored row: %v", ip)
			ip.Operation = proto.DbOperationForThisRow_DELETE
			continue
		}

		cached, found := ipCache[ip.IpAddress]
		if !found {
			if circuitOpen {
				continue
			}

			ipInfo, err := lookupFn(ip.IpAddress)
			if err != nil {
				if errors.Is(err, third_party.ErrPermanentLookup) {
					log.Printf("Permanent geolocation failure for IP %s, marking IGNORE: %v", ip.IpAddress, err)
					cached = cachedLookup{permanent: true}
					ipCache[ip.IpAddress] = cached
					consecutiveTransientErrors = 0
					sleepFn(lookupSuccessDelay)
				} else {
					log.Printf("Transient error fetching info for IP %s: %v", ip.IpAddress, err)
					cached = cachedLookup{transient: true}
					ipCache[ip.IpAddress] = cached
					consecutiveTransientErrors++
					if consecutiveTransientErrors >= maxConsecutiveTransientErrors {
						log.Printf("Reached %d consecutive transient geolocation errors; pausing remaining lookups for this run", consecutiveTransientErrors)
						circuitOpen = true
					} else {
						sleepFn(lookupErrorDelay)
					}
					continue
				}
			} else {
				cached = cachedLookup{info: ipInfo}
				ipCache[ip.IpAddress] = cached
				consecutiveTransientErrors = 0
				sleepFn(lookupSuccessDelay)
			}
		} else {
			log.Printf("[CACHE HIT] Reusing lookup for IP: %s", ip.IpAddress)
		}

		if cached.permanent {
			ip.Operation = proto.DbOperationForThisRow_IGNORE
			continue
		}
		if cached.transient || cached.info == nil {
			continue
		}

		applyLocation(ip, cached.info)
	}
}

func applyLocation(ip *proto.IpCheckDto, ipInfo *third_party.IPInfo) {
	countryName := strings.TrimSpace(ipInfo.Country)
	if countryName == "" {
		countryName = "Unknown"
	}

	ip.Country = fmt.Sprintf("%s - %s", GetFlag(ipInfo.CountryCode), countryName)
	ip.City = strings.TrimSpace(ipInfo.City)
	ip.Operation = proto.DbOperationForThisRow_UPDATE
}

func hasProcessedRows(ips []*proto.IpCheckDto) bool {
	for _, ip := range ips {
		if ip.Operation != proto.DbOperationForThisRow_UNPROCESSED {
			return true
		}
	}
	return false
}

func logTheResult(ips []*proto.IpCheckDto) {
	deletedCount := countByOperation(ips, proto.DbOperationForThisRow_DELETE)
	updatedCount := countByOperation(ips, proto.DbOperationForThisRow_UPDATE)
	ignoredCount := countByOperation(ips, proto.DbOperationForThisRow_IGNORE)
	unprocessedCount := countByOperation(ips, proto.DbOperationForThisRow_UNPROCESSED)

	log.Println("ip-lookup-cron completed successfully.")
	log.Printf("Total rows: %d | Updated: %d | Deleted: %d | Ignored: %d | Unprocessed: %d\n",
		len(ips), updatedCount, deletedCount, ignoredCount, unprocessedCount)
}

func countByOperation(ips []*proto.IpCheckDto, op proto.DbOperationForThisRow) int {
	count := 0
	for _, ip := range ips {
		if ip.Operation == op {
			count++
		}
	}
	return count
}
