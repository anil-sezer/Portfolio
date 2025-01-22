package grpc

import (
	pb "ip-lookup-cron/proto"
	"log"
)

func SendIpCheckResultsToGrpc(ips []*pb.IpCheckDto) {

	client, context, cancel := getClientAndContext()
	defer cancel()

	res, err := client.PersistCheckedIps(context, &pb.PersistCheckedIpsRequest{CheckedIps: ips})
	if err != nil {
		log.Fatalf("Cannot persist ip check results: %v", err)
	}

	log.Printf("Response: %v", res)
}
