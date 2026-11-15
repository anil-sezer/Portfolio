package grpc

import (
	"context"
	pb "ip-lookup-cron/proto"
	"log"
)

func sendIpCheckResults(ips []*pb.IpCheckDto) error {
	return withClient(func(ctx context.Context, client pb.VisitorInsightsClient) error {
		res, err := client.PersistCheckedIps(ctx, &pb.PersistCheckedIpsRequest{CheckedIps: ips})
		if err != nil {
			return err
		}

		log.Printf("Response: %v", res)
		return nil
	})
}

func SendIpCheckResultsToGrpc(ips []*pb.IpCheckDto) {
	if err := sendIpCheckResults(ips); err != nil {
		log.Fatalf("Cannot persist ip check results: %v", err)
	}
}
