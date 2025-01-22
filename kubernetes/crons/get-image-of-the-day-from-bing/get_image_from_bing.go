package main

import (
	"encoding/json"
	"fmt"
	"io"
	"log"
	"net/http"
)

type bingImageResponse struct {
	Images []struct {
		URLBase string `json:"urlbase"`
	} `json:"images"`
}

func GetDailyImageUrl() (string, error) {
	jsonObject, err := downloadJson()
	if err != nil {
		return "", err
	}

	urlBase := "https://www.bing.com" + jsonObject.Images[0].URLBase
	return urlBase + "_1920x1080.jpg", nil
}

func downloadJson() (*bingImageResponse, error) {
	const locale = "tr-TR"
	url := fmt.Sprintf("https://www.bing.com/HPImageArchive.aspx?format=js&idx=0&n=1&mkt=%s", locale)

	log.Println("Downloading JSON...")

	resp, err := http.Get(url)
	if err != nil {
		log.Printf("Failed to fetch JSON: %v", err)
		return nil, err
	}
	defer resp.Body.Close()

	if resp.StatusCode != http.StatusOK {
		log.Printf("Non-OK HTTP status: %d", resp.StatusCode)
		return nil, fmt.Errorf("HTTP request failed with status: %d", resp.StatusCode)
	}

	body, err := io.ReadAll(resp.Body)
	if err != nil {
		log.Printf("Failed to read response body: %v", err)
		return nil, err
	}

	log.Printf("JSON string: %s", string(body))

	var jsonObject bingImageResponse
	err = json.Unmarshal(body, &jsonObject)
	if err != nil {
		log.Printf("Failed to parse JSON: %v", err)
		return nil, err
	}

	return &jsonObject, nil
}
