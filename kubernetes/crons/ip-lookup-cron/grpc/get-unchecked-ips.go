package grpc

import (
	"context"
	pb "ip-lookup-cron/proto"
	"log"

	"github.com/golang/protobuf/ptypes/empty"
)

func getIpsToCheck() ([]*pb.IpCheckDto, error) {
	var ips []*pb.IpCheckDto
	err := withClient(func(ctx context.Context, client pb.VisitorInsightsClient) error {
		res, err := client.GetIpsToCheck(ctx, &empty.Empty{})
		if err != nil {
			return err
		}

		log.Printf("Response: %v", res)
		log.Printf("Got %d IPs from gRPC", len(res.Ips))
		ips = res.Ips
		return nil
	})
	return ips, err
}

func GetIpsToCheckFromGrpc() []*pb.IpCheckDto {
	ips, err := getIpsToCheck()
	if err != nil {
		log.Fatalf("Could not get ips to check: %v", err)
	}
	return ips
}
