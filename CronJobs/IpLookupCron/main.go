package main

import (
	"IpLookupCron/Constants"
	"IpLookupCron/DataAccess"
	"IpLookupCron/DataAccess/Entities/Postgres"
	"IpLookupCron/DataAccess/ThirdPartyApiCalls/GetIpInfo"
	"fmt"
	"github.com/joho/godotenv"
	"time"
)

func main() {
	if err := godotenv.Load(); err != nil {
		fmt.Println("Env file not present")
	}

	var db = DataAccess.DbContext()

	// Query all requests from the database into a slice of Request structs.
	var requests []Postgres.Request
	result := db.Where("country = ?", "").Find(&requests)
	if result.Error != nil {
		panic("failed to query requests")
	}

	// Iterate over the slice and perform some operations (e.g., update the country and city based on the client IP).
	for _, req := range requests {
		// Assume getIPInfo is a function that returns country and city information for a given IP.

		if req.ClientIP == "31.223.32.192" {
			req.Country = "🏠 - Me"
			req.City = "Istanbul"

			// Save the updated request back to the database.
			db.Save(&req)
			continue
		}

		if req.ClientIP == "127.0.0.1" {
			req.Country = "🖥️ - Me"
			req.City = "Istanbul"

			// Save the updated request back to the database.
			db.Save(&req)
			continue
		}

		ipInfo, err := GetIpInfo.GetIPInfo(req.ClientIP)
		if err != nil {
			time.Sleep(time.Second * 10)
			fmt.Println(err)
			continue
		}

		// Update the country and city fields of the Request object.
		req.Country = Constants.GetFlag(ipInfo.CountryCode) + " - " + ipInfo.Country
		// req.Country = ipInfo.Country
		req.City = ipInfo.City

		// Save the updated request back to the database.
		db.Save(&req)
		time.Sleep(time.Second) // Rate limit the request
	}

	fmt.Println("IpLookupCron is done.")
}
