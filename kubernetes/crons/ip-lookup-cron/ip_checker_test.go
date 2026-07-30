package main

import (
	"testing"

	"ip-lookup-cron/proto"
)

func TestIsPrivateOrIgnoredIP(t *testing.T) {
	tests := []struct {
		ip       string
		expected bool
	}{
		{"127.0.0.1", true},
		{"10.0.0.1", true},
		{"172.16.0.5", true},
		{"192.168.1.100", true},
		{"169.254.1.1", true},
		{"0.0.0.0", true},
		{"::1", true},
		{"31.223.32.192", true}, // custom ignored IP
		{"invalid-ip", true},
		{"8.8.8.8", false},
		{"1.1.1.1", false},
	}

	for _, tt := range tests {
		result := isPrivateOrIgnoredIP(tt.ip)
		if result != tt.expected {
			t.Errorf("isPrivateOrIgnoredIP(%q) = %v; want %v", tt.ip, result, tt.expected)
		}
	}
}

func TestProcessIPsPrivateOnly(t *testing.T) {
	testIPs := []*proto.IpCheckDto{
		{IpAddress: "127.0.0.1"},
		{IpAddress: "192.168.1.50"},
		{IpAddress: "31.223.32.192"},
	}

	processIPs(testIPs)

	deletedCount := countByOperation(testIPs, proto.DbOperationForThisRow_DELETE)
	if deletedCount != 3 {
		t.Errorf("Expected 3 deleted rows, got %d", deletedCount)
	}

	for _, ip := range testIPs {
		if ip.Operation != proto.DbOperationForThisRow_DELETE {
			t.Errorf("Expected operation DELETE for IP %s, got %v", ip.IpAddress, ip.Operation)
		}
	}
}
