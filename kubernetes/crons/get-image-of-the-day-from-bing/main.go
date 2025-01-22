package main

import (
	"fmt"
	"github.com/joho/godotenv"
	"log"
)

func main() {
	if err := godotenv.Load(); err != nil {
		log.Println("Env file not present.")
	}

	url, err := GetDailyImageUrl()
	if err != nil {
		log.Fatalf("Failed to get daily image URL: %v", err)
	}
	fmt.Println("Daily Image URL:", url)

	if IsValidImageUrl(url) == false {
		log.Fatalf("Failed to get daily image URL: %v", err)
	}

	SendUrlToGrpc(url)
}
