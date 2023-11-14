package main

import (
	"IpLookupCron/Constants"
	"IpLookupCron/DataAccess/Entities/Postgres"
	"IpLookupCron/ThirdPartyApiCalls/GetIpInfo"
	"fmt"
	"gorm.io/driver/postgres"
	"gorm.io/gorm"
	"time"
)

func DbContext() *gorm.DB {
	// Initialize the ORM database connection ('dsn' is your Postgres connection string).
	dsn := "host=192.168.1.104 port=30001 user=default-user password=rgT6%Qk9jTaURwK!& dbname=postgres sslmode=disable TimeZone=Europe/Istanbul"
	db, err := gorm.Open(postgres.Open(dsn), &gorm.Config{})
	if err != nil {
		panic("failed to connect to database")
	}

	return db
}

func AutoMigrate(db *gorm.DB) {

	// Automatically migrate your schema, creating the 'requests' table if it doesn't exist.
	var err = db.AutoMigrate(&Postgres.Request{})
	if err != nil {
		panic("failed to auto migrate")
	}
}

func main() {

	var db = DbContext()

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
