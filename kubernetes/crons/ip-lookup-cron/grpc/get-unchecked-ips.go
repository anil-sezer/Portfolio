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

// GetTestIps todo: Use this in a unit test instead of here
func GetTestIps() []*pb.IpCheckDto {
	return []*pb.IpCheckDto{
		{
			IpAddress: "127.0.0.1",
			Country:   "Localhost",
			City:      "Local",
			Operation: pb.DbOperationForThisRow_UPDATE,
		},
		{
			IpAddress: "31.223.32.192",
			Country:   "Test Country",
			City:      "Test City",
			Operation: pb.DbOperationForThisRow_UPDATE,
		},
		{
			IpAddress: "8.8.8.8",
			Country:   "United States",
			City:      "Mountain View",
			Operation: pb.DbOperationForThisRow_UNPROCESSED,
		},
	}
}
