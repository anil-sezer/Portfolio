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

const grpcTimeout = 15 * time.Second

func setupConnection() *grpc.ClientConn {
	grpcAddress, _ := os.LookupEnv("GRPC_BASE_URL")

	log.Printf("Connecting to: %s", grpcAddress)
	conn, err := grpc.NewClient(grpcAddress, grpc.WithTransportCredentials(insecure.NewCredentials()))
	if err != nil {
		log.Fatalf("Did not connect: %v", err)
	}
	return conn
}

func withClient(fn func(ctx context.Context, client pb.VisitorInsightsClient) error) error {
	conn := setupConnection()
	defer func() {
		if err := conn.Close(); err != nil {
			log.Printf("Failed to close gRPC connection: %v", err)
		}
	}()

	client := pb.NewVisitorInsightsClient(conn)

	ctx, cancel := context.WithTimeout(context.Background(), grpcTimeout)
	defer cancel()

	return fn(ctx, client)
}
