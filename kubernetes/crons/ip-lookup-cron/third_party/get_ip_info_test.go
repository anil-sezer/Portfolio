package third_party

import (
	"net/http"
	"net/http/httptest"
	"strings"
	"testing"
	"time"
)

func TestDefaultClientAndURLConfig(t *testing.T) {
	if !strings.HasPrefix(defaultBaseURL, "https://") {
		t.Fatalf("expected defaultBaseURL to use HTTPS, got %q", defaultBaseURL)
	}
	if httpClient.Timeout != 5*time.Second {
		t.Fatalf("expected httpClient.Timeout to be 5s, got %v", httpClient.Timeout)
	}
}

func TestGetIPInfoWithClient_Success(t *testing.T) {
	srv := httptest.NewServer(http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		if r.URL.Path != "/8.8.8.8" {
			t.Fatalf("unexpected request path: %s", r.URL.Path)
		}
		w.Header().Set("Content-Type", "application/json")
		_, _ = w.Write([]byte(`{
			"ip": "8.8.8.8",
			"success": true,
			"country": "United States",
			"country_code": "US",
			"city": "San Jose"
		}`))
	}))
	defer srv.Close()

	info, err := getIPInfoWithClient(srv.Client(), srv.URL, "8.8.8.8")
	if err != nil {
		t.Fatalf("expected no error, got %v", err)
	}
	if info.CountryCode != "US" || info.Country != "United States" || info.City != "San Jose" {
		t.Fatalf("unexpected IPInfo: %+v", info)
	}
}

func TestGetIPInfoWithClient_APIFailure(t *testing.T) {
	srv := httptest.NewServer(http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		w.Header().Set("Content-Type", "application/json")
		_, _ = w.Write([]byte(`{"ip":"127.0.0.1","success":false,"message":"Reserved range"}`))
	}))
	defer srv.Close()

	info, err := getIPInfoWithClient(srv.Client(), srv.URL, "127.0.0.1")
	if err == nil {
		t.Fatalf("expected error for success=false, got info=%+v", info)
	}
	if !strings.Contains(err.Error(), "Reserved range") {
		t.Fatalf("expected error message to include API failure reason, got %v", err)
	}
}

func TestGetIPInfoWithClient_Non200Status(t *testing.T) {
	srv := httptest.NewServer(http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		w.WriteHeader(http.StatusTooManyRequests)
		_, _ = w.Write([]byte(`rate limit exceeded`))
	}))
	defer srv.Close()

	_, err := getIPInfoWithClient(srv.Client(), srv.URL, "8.8.8.8")
	if err == nil {
		t.Fatal("expected error for HTTP 429 response, got nil")
	}
	if !strings.Contains(err.Error(), "429") {
		t.Fatalf("expected error to mention status 429, got %v", err)
	}
}

func TestGetIPInfoWithClient_Timeout(t *testing.T) {
	srv := httptest.NewServer(http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		time.Sleep(150 * time.Millisecond)
		w.WriteHeader(http.StatusOK)
	}))
	defer srv.Close()

	client := &http.Client{Timeout: 30 * time.Millisecond}
	_, err := getIPInfoWithClient(client, srv.URL, "8.8.8.8")
	if err == nil {
		t.Fatal("expected timeout error, got nil")
	}
}
