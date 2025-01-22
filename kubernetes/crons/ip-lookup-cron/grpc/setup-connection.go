package grpc

import (
	"context"
	"google.golang.org/grpc"
	"google.golang.org/grpc/credentials/insecure"
	pb "ip-lookup-cron/proto"
	"log"
	"os"
	"time"
)

func setupConnection() *grpc.ClientConn {
	grpcAddress, _ := os.LookupEnv("GRPC_BASE_URL")

	conn, err := grpc.NewClient(grpcAddress, grpc.WithTransportCredentials(insecure.NewCredentials()))
	if err != nil {
		log.Fatalf("Did not connect: %v", err)
	}
	return conn
}

func getClientAndContext() (pb.VisitorInsightsClient, context.Context, context.CancelFunc) {
	conn := setupConnection()
	client := pb.NewVisitorInsightsClient(conn)

	ctx, cancel := context.WithTimeout(context.Background(), time.Second*2)

	return client, ctx, cancel
}
