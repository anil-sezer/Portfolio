package main

import (
	"encoding/json"
	"fmt"
	"io"
	"log"
	"net/http"
	"strings"
)

type bingImageResponse struct {
	Images []struct {
		URLBase   string `json:"urlbase"`
		Title     string `json:"title"`
		Copyright string `json:"copyright"`
	} `json:"images"`
}

func GetDailyImageDetails() (string, string, error) {
	jsonObject, err := downloadJson()
	if err != nil {
		return "", "", err
	}

	if len(jsonObject.Images) == 0 {
		return "", "", fmt.Errorf("no images found in Bing response")
	}

	img := jsonObject.Images[0]
	urlBase := "https://www.bing.com" + img.URLBase
	altText := buildAltText(img.Title, img.Copyright)

	return urlBase + "_1920x1080.jpg", altText, nil
}

func buildAltText(title, copyright string) string {
	title = strings.TrimSpace(title)
	copyright = strings.TrimSpace(copyright)

	if title != "" && copyright != "" {
		if strings.HasPrefix(strings.ToLower(copyright), strings.ToLower(title)) {
			return copyright
		}
		return fmt.Sprintf("%s - %s", title, copyright)
	}
	if copyright != "" {
		return copyright
	}
	if title != "" {
		return title
	}
	return "Daily featured background photograph from Bing Wallpaper"
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
