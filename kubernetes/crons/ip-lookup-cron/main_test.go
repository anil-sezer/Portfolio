package main

import (
	"errors"
	"fmt"
	"testing"
	"time"

	"ip-lookup-cron/proto"
	"ip-lookup-cron/third_party"
)

func TestGetFlag(t *testing.T) {
	tests := []struct {
		code string
		want string
	}{
		{code: "US", want: "🇺🇸"},
		{code: " tr ", want: "🇹🇷"},
		{code: "XX", want: "🏳"},
		{code: "ZZ", want: "🌐"},
		{code: "", want: "🌐"},
	}

	for _, tt := range tests {
		if got := GetFlag(tt.code); got != tt.want {
			t.Errorf("GetFlag(%q) = %q, want %q", tt.code, got, tt.want)
		}
	}
}

func TestProcessIPsWithDeps_PermanentErrorMarksIgnoreAndCaches(t *testing.T) {
	ips := []*proto.IpCheckDto{
		{EntityId: 1, IpAddress: "192.0.2.1"},
		{EntityId: 2, IpAddress: "192.0.2.1"},
		{EntityId: 3, IpAddress: "8.8.8.8"},
	}

	lookupCalls := map[string]int{}
	lookupFn := func(ip string) (*third_party.IPInfo, error) {
		lookupCalls[ip]++
		if ip == "192.0.2.1" {
			return nil, fmt.Errorf("%w: reserved range", third_party.ErrPermanentLookup)
		}
		return &third_party.IPInfo{
			IP:          ip,
			Success:     true,
			Country:     "United States",
			CountryCode: "US",
			City:        "Mountain View",
		}, nil
	}

	processIPsWithDeps(ips, lookupFn, func(time.Duration) {})

	if lookupCalls["192.0.2.1"] != 1 {
		t.Fatalf("expected 1 lookup for cached poison IP, got %d", lookupCalls["192.0.2.1"])
	}
	if ips[0].Operation != proto.DbOperationForThisRow_IGNORE {
		t.Errorf("row 0 operation = %v, want IGNORE", ips[0].Operation)
	}
	if ips[1].Operation != proto.DbOperationForThisRow_IGNORE {
		t.Errorf("row 1 operation = %v, want IGNORE", ips[1].Operation)
	}
	if ips[2].Operation != proto.DbOperationForThisRow_UPDATE {
		t.Errorf("row 2 operation = %v, want UPDATE", ips[2].Operation)
	}
	if ips[2].Country != "🇺🇸 - United States" || ips[2].City != "Mountain View" {
		t.Errorf("row 2 location = (%q, %q), unexpected", ips[2].City, ips[2].Country)
	}
}

func TestProcessIPsWithDeps_TransientErrorsTripCircuitBreaker(t *testing.T) {
	ips := []*proto.IpCheckDto{
		{EntityId: 1, IpAddress: "1.1.1.1"},
		{EntityId: 2, IpAddress: "8.8.4.4"},
		{EntityId: 3, IpAddress: "9.9.9.9"},
		{EntityId: 4, IpAddress: "9.9.9.9"},
		{EntityId: 5, IpAddress: "4.2.2.1"},
		{EntityId: 6, IpAddress: "4.2.2.2"}, // Should be skipped by circuit breaker
		{EntityId: 7, IpAddress: "1.1.1.1"}, // Should still hit cache
		{EntityId: 8, IpAddress: "10.0.0.5"}, // Private IP still marked DELETE
	}

	lookupCalls := 0
	lookupFn := func(ip string) (*third_party.IPInfo, error) {
		lookupCalls++
		if ip == "1.1.1.1" {
			return &third_party.IPInfo{
				IP:          ip,
				Success:     true,
				Country:     "",
				CountryCode: "ZZ",
				City:        "Anycast",
			}, nil
		}
		return nil, errors.New("upstream 503")
	}

	processIPsWithDeps(ips, lookupFn, func(time.Duration) {})

	// 1.1.1.1 (ok) + 8.8.4.4 (err 1) + 9.9.9.9 (err 2, cached for id 4) + 4.2.2.1 (err 3 -> opens circuit)
	if lookupCalls != 4 {
		t.Fatalf("expected 4 lookup calls before circuit opened, got %d", lookupCalls)
	}
	if ips[0].Operation != proto.DbOperationForThisRow_UPDATE || ips[0].Country != "🌐 - Unknown" {
		t.Errorf("row 0 = (%v, %q), want (UPDATE, %q)", ips[0].Operation, ips[0].Country, "🌐 - Unknown")
	}
	for _, idx := range []int{1, 2, 3, 4, 5} {
		if ips[idx].Operation != proto.DbOperationForThisRow_UNPROCESSED {
			t.Errorf("row %d operation = %v, want UNPROCESSED", idx, ips[idx].Operation)
		}
	}
	if ips[6].Operation != proto.DbOperationForThisRow_UPDATE {
		t.Errorf("row 6 cached hit operation = %v, want UPDATE", ips[6].Operation)
	}
	if ips[7].Operation != proto.DbOperationForThisRow_DELETE {
		t.Errorf("row 7 private IP operation = %v, want DELETE", ips[7].Operation)
	}
}

func TestHasProcessedRows(t *testing.T) {
	unprocessed := []*proto.IpCheckDto{
		{EntityId: 1, Operation: proto.DbOperationForThisRow_UNPROCESSED},
	}
	if hasProcessedRows(unprocessed) {
		t.Fatal("expected hasProcessedRows to return false when all rows are UNPROCESSED")
	}

	ignored := []*proto.IpCheckDto{
		{EntityId: 1, Operation: proto.DbOperationForThisRow_UNPROCESSED},
		{EntityId: 2, Operation: proto.DbOperationForThisRow_IGNORE},
	}
	if !hasProcessedRows(ignored) {
		t.Fatal("expected hasProcessedRows to return true when at least one row is IGNORE")
	}
}
