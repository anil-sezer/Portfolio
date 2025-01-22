package main

import (
	"fmt"
	"github.com/joho/godotenv"
	"ip-lookup-cron/grpc"
	"ip-lookup-cron/proto"
	"ip-lookup-cron/third_party"
	"time"
)

func main() {
	if err := godotenv.Load(); err != nil {
		fmt.Println("Env file not present")
	}

	ips := grpc.GetIpsToCheckFromGrpc()
	fmt.Println("Got: " + fmt.Sprintf("%d", len(ips)) + " from grpc")

	rowsToDeleteCount := 0
	for _, ip := range ips {
		if ip.IpAddress == "127.0.0.1" ||
			ip.IpAddress == "31.223.32.192" {
			fmt.Println("Will delete this row: " + fmt.Sprintf("%v", ip))
			rowsToDeleteCount++
			ip.Operation = proto.DbOperationForThisRow_DELETE
			continue
		}

		ipInfo, err := third_party.GetIPInfo(ip.IpAddress)
		if err != nil {
			time.Sleep(time.Second * 10)
			fmt.Println(err)
			continue
		}

		ip.Country = GetFlag(ipInfo.CountryCode) + " - " + ipInfo.Country
		ip.City = ipInfo.City
		ip.Operation = proto.DbOperationForThisRow_UPDATE

		time.Sleep(time.Second * 2) // Rate limit the request
	}

	if len(ips) > 0 {
		grpc.SendIpCheckResultsToGrpc(ips)
	}

	fmt.Println("ip-lookup-cron is done.")
	fmt.Println("Will affect: " + fmt.Sprintf("%d", len(ips)) + " rows")
	fmt.Println("Will delete: " + fmt.Sprintf("%d", rowsToDeleteCount) + " rows")
}
