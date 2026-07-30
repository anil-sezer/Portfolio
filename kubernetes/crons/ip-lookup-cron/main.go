package main

import (
	"fmt"
	"log"
	"time"

	"ip-lookup-cron/grpc"
	"ip-lookup-cron/proto"
	"ip-lookup-cron/third_party"

	"github.com/joho/godotenv"
)

func main() {
	loadEnvValues()

	ips := grpc.GetIpsToCheckFromGrpc()
	if len(ips) == 0 {
		log.Println("No IPs to process. Exiting.")
		return
	}

	processIPs(ips)

	grpc.SendIpCheckResultsToGrpc(ips)

	logTheResult(ips)
}

func loadEnvValues() {
	if err := godotenv.Load(); err != nil {
		log.Println("Env file not present, using system defaults")
	}
}

func processIPs(ips []*proto.IpCheckDto) {
	ipCache := make(map[string]*third_party.IPInfo)

	for _, ip := range ips {
		if isPrivateOrIgnoredIP(ip.IpAddress) {
			log.Printf("Will delete private/ignored row: %v", ip)
			ip.Operation = proto.DbOperationForThisRow_DELETE
			continue
		}

		var ipInfo *third_party.IPInfo
		if cachedInfo, found := ipCache[ip.IpAddress]; found {
			log.Printf("[CACHE HIT] Reusing lookup for IP: %s", ip.IpAddress)
			ipInfo = cachedInfo
		} else {
			var err error
			ipInfo, err = third_party.GetIPInfo(ip.IpAddress)
			if err != nil {
				log.Printf("Error fetching info for IP %s: %v", ip.IpAddress, err)
				time.Sleep(10 * time.Second)
				continue
			}
			ipCache[ip.IpAddress] = ipInfo
			time.Sleep(2 * time.Second)
		}

		ip.Country = fmt.Sprintf("%s - %s", GetFlag(ipInfo.CountryCode), ipInfo.Country)
		ip.City = ipInfo.City
		ip.Operation = proto.DbOperationForThisRow_UPDATE
	}
}

func logTheResult(ips []*proto.IpCheckDto) {
	deletedCount := countByOperation(ips, proto.DbOperationForThisRow_DELETE)
	updatedCount := countByOperation(ips, proto.DbOperationForThisRow_UPDATE)

	log.Println("ip-lookup-cron completed successfully.")
	log.Printf("Total rows: %d | Updated: %d | Deleted: %d\n", len(ips), updatedCount, deletedCount)
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
