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

	processBingImage()
	log.Println("✅ Bing image processing completed successfully.")
	//todo: Add NASA APOD API +
	// Eurpean Space Agency (ESA) Monthly image
	// https://esawebb.org/images/potm/
}

func processBingImage() {
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
