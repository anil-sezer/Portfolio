package main

import (
	"log"
	"net/http"
	"strings"
	"time"
)

func IsValidImageUrl(imageUrl string) bool {
	client := &http.Client{
		Timeout: 10 * time.Second,
	}

	resp, err := client.Get(imageUrl)
	if err != nil {
		log.Printf("Background image URL is not valid: %s", imageUrl)
		return false
	}
	defer resp.Body.Close()

	if resp.StatusCode != http.StatusOK {
		log.Printf("Background image URL is not valid: %s, HttpStatusCode: %d", imageUrl, resp.StatusCode)
		return false
	}

	contentType := resp.Header.Get("Content-Type")
	if contentType != "" && strings.HasPrefix(contentType, "image/") {
		return true
	}

	log.Printf("Background image URL is not valid: %s, HttpStatusCode: %d", imageUrl, resp.StatusCode)
	return false
}
