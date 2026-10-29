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
	url, altText, err := GetDailyImageDetails()
	if err != nil {
		log.Fatalf("Failed to get daily image URL: %v", err)
	}
	fmt.Println("Daily Image URL:", url)
	fmt.Println("Daily Image AltText:", altText)

	if !IsValidImageUrl(url) {
		log.Fatalf("Daily image URL is not valid: %s", url)
	}

	SendUrlToGrpc(url, altText)
}
