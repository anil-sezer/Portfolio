package third_party

import (
	"encoding/json"
	"errors"
	"fmt"
	"io"
	"log"
	"net/http"
	"net/url"
	"strings"
	"time"
)

const defaultBaseURL = "https://ipwho.is"

// ErrPermanentLookup indicates the IP address cannot be geolocated (e.g., reserved range, invalid IP, or empty location).
var ErrPermanentLookup = errors.New("permanent geolocation lookup failure")

var httpClient = &http.Client{
	Timeout: 5 * time.Second,
}

type IPInfo struct {
	IP          string  `json:"ip"`
	Success     bool    `json:"success"`
	Message     string  `json:"message"`
	Country     string  `json:"country"`
	CountryCode string  `json:"country_code"`
	Region      string  `json:"region"`
	RegionCode  string  `json:"region_code"`
	City        string  `json:"city"`
	Postal      string  `json:"postal"`
	Latitude    float64 `json:"latitude"`
	Longitude   float64 `json:"longitude"`
}

// GetIPInfo fetches geolocation metadata for the given IP address over HTTPS.
func GetIPInfo(ip string) (*IPInfo, error) {
	return getIPInfoWithClient(httpClient, defaultBaseURL, ip)
}

func getIPInfoWithClient(client *http.Client, baseURL, ip string) (*IPInfo, error) {
	requestURL := fmt.Sprintf("%s/%s", strings.TrimRight(baseURL, "/"), url.PathEscape(ip))

	log.Println("[INFO] - Will request to this url: " + requestURL)

	resp, err := client.Get(requestURL)
	if err != nil {
		return nil, fmt.Errorf("error fetching IP information: %w", err)
	}
	defer resp.Body.Close()

	body, err := io.ReadAll(resp.Body)
	if err != nil {
		return nil, fmt.Errorf("error reading response body: %w", err)
	}

	if resp.StatusCode != http.StatusOK {
		if resp.StatusCode >= http.StatusBadRequest && resp.StatusCode < http.StatusInternalServerError && resp.StatusCode != http.StatusTooManyRequests {
			return nil, fmt.Errorf("%w: unexpected HTTP status %d from geolocation API: %s", ErrPermanentLookup, resp.StatusCode, string(body))
		}
		return nil, fmt.Errorf("unexpected HTTP status %d from geolocation API: %s", resp.StatusCode, string(body))
	}

	log.Println("[INFO] - Response: " + string(body))
	log.Println()

	var ipInfo IPInfo
	if err := json.Unmarshal(body, &ipInfo); err != nil {
		return nil, fmt.Errorf("error parsing JSON: %w", err)
	}

	if !ipInfo.Success {
		return nil, fmt.Errorf("%w: geolocation lookup failed for IP %s: %s", ErrPermanentLookup, ip, ipInfo.Message)
	}

	if strings.TrimSpace(ipInfo.Country) == "" && strings.TrimSpace(ipInfo.City) == "" {
		return nil, fmt.Errorf("%w: geolocation API returned empty country and city for IP %s", ErrPermanentLookup, ip)
	}

	return &ipInfo, nil
}
