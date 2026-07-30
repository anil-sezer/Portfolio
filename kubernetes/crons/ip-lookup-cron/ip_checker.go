package main

import "net/netip"

// customIgnoredIPs contains specific public IPs that should also be ignored/deleted.
var customIgnoredIPs = map[string]bool{
	"31.223.32.192": true,
}

// isPrivateOrIgnoredIP checks whether an IP address string represents:
// 1. An invalid/unparseable IP
// 2. A private IP range (RFC 1918 IPv4, IPv6 ULA)
// 3. A loopback address (127.0.0.0/8, ::1)
// 4. An unspecified or zero address (0.0.0.0, ::)
// 5. A link-local address (169.254.0.0/16, fe80::/10)
// 6. An explicitly ignored custom IP
func isPrivateOrIgnoredIP(ipStr string) bool {
	addr, err := netip.ParseAddr(ipStr)
	if err != nil {
		return true
	}

	if addr.IsPrivate() || addr.IsLoopback() || addr.IsUnspecified() || addr.IsLinkLocalUnicast() {
		return true
	}

	return customIgnoredIPs[ipStr]
}
