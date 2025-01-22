package grpc

import (
	"github.com/golang/protobuf/ptypes/empty"
	pb "ip-lookup-cron/proto"
	"log"
)

func GetIpsToCheckFromGrpc() []*pb.IpCheckDto {
	client, context, cancel := getClientAndContext()
	defer cancel()

	res, err := client.GetIpsToCheck(context, &empty.Empty{})
	if err != nil {
		log.Fatalf("Could not get ips to check: %v", err)
	}

	log.Printf("Response: %v", res)

	return res.Ips
}
