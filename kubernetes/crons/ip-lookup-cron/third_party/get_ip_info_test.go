package third_party

import (
	"errors"
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
	if !errors.Is(err, ErrPermanentLookup) {
		t.Fatalf("expected ErrPermanentLookup for success=false, got %v", err)
	}
	if !strings.Contains(err.Error(), "Reserved range") {
		t.Fatalf("expected error message to include API failure reason, got %v", err)
	}
}

func TestGetIPInfoWithClient_EmptyLocationIsPermanent(t *testing.T) {
	srv := httptest.NewServer(http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		w.Header().Set("Content-Type", "application/json")
		_, _ = w.Write([]byte(`{"ip":"203.0.113.1","success":true,"country":"","country_code":"","city":""}`))
	}))
	defer srv.Close()

	_, err := getIPInfoWithClient(srv.Client(), srv.URL, "203.0.113.1")
	if !errors.Is(err, ErrPermanentLookup) {
		t.Fatalf("expected ErrPermanentLookup for empty country and city, got %v", err)
	}
}

func TestGetIPInfoWithClient_StatusCodeClassification(t *testing.T) {
	tests := []struct {
		name          string
		statusCode    int
		wantPermanent bool
	}{
		{name: "400 is permanent", statusCode: http.StatusBadRequest, wantPermanent: true},
		{name: "404 is permanent", statusCode: http.StatusNotFound, wantPermanent: true},
		{name: "429 is transient", statusCode: http.StatusTooManyRequests, wantPermanent: false},
		{name: "503 is transient", statusCode: http.StatusServiceUnavailable, wantPermanent: false},
	}

	for _, tt := range tests {
		t.Run(tt.name, func(t *testing.T) {
			srv := httptest.NewServer(http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
				w.WriteHeader(tt.statusCode)
				_, _ = w.Write([]byte(`error body`))
			}))
			defer srv.Close()

			_, err := getIPInfoWithClient(srv.Client(), srv.URL, "8.8.8.8")
			if err == nil {
				t.Fatalf("expected error for HTTP %d, got nil", tt.statusCode)
			}
			if got := errors.Is(err, ErrPermanentLookup); got != tt.wantPermanent {
				t.Fatalf("HTTP %d permanent=%v, want %v (err=%v)", tt.statusCode, got, tt.wantPermanent, err)
			}
		})
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
