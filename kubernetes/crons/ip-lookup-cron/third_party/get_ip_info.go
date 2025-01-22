package third_party

import (
	"encoding/json"
	"fmt"
	"io/ioutil"
	"log"
	"net/http"
)

type IPInfo struct {
	Query       string  `json:"query"`
	Status      string  `json:"status"`
	Country     string  `json:"country"`
	CountryCode string  `json:"countryCode"`
	Region      string  `json:"region"`
	RegionName  string  `json:"regionName"`
	City        string  `json:"city"`
	Zip         string  `json:"zip"`
	Lat         float64 `json:"lat"`
	Lon         float64 `json:"lon"`
	Timezone    string  `json:"timezone"`
	Isp         string  `json:"isp"`
	Org         string  `json:"org"`
	As          string  `json:"as"`
}

func GetIPInfo(ip string) (*IPInfo, error) {
	// Construct the URL with the specified IP address
	url := fmt.Sprintf("http://ip-api.com/json/%s", ip)

	log.Println("[INFO] - Will request to this url: " + url)

	// Make a GET request to the API
	resp, err := http.Get(url)
	if err != nil {
		return nil, fmt.Errorf("error fetching IP information: %w", err)
	}
	defer resp.Body.Close()

	// Read the response body
	body, err := ioutil.ReadAll(resp.Body)
	if err != nil {
		return nil, fmt.Errorf("error reading response body: %w", err)
	}

	log.Println("[INFO] - Response: " + string(body))
	log.Println()

	// Unmarshal the JSON data into the IPInfo struct
	var ipInfo IPInfo
	if err := json.Unmarshal(body, &ipInfo); err != nil {

		return nil, fmt.Errorf("error parsing JSON: %w", err)
	}

	// Return the IPInfo struct
	return &ipInfo, nil
}
