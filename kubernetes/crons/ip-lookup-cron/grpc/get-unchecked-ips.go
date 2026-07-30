package grpc

import (
	pb "ip-lookup-cron/proto"
	"log"

	"github.com/golang/protobuf/ptypes/empty"
)

func GetIpsToCheckFromGrpc() []*pb.IpCheckDto {
	client, context, cancel := getClientAndContext()
	defer cancel()

	res, err := client.GetIpsToCheck(context, &empty.Empty{})
	if err != nil {
		log.Fatalf("Could not get ips to check: %v", err)
	}

	log.Printf("Response: %v", res)

	log.Printf("Got %d IPs from gRPC", len(res.Ips))

	return res.Ips
}
