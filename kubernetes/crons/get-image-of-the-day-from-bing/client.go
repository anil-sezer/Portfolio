package main

import (
	"context"
	"google.golang.org/grpc/credentials/insecure"
	"log"
	"os"
	"time"

	pb "get-image-of-the-day-from-bing/proto"
	"google.golang.org/grpc"
)

const persistTimeout = 10 * time.Second

func setupConnection() *grpc.ClientConn {
	grpcAddress, _ := os.LookupEnv("GRPC_BASE_URL")

	log.Printf("Connecting to: %s", grpcAddress)
	conn, err := grpc.NewClient(grpcAddress, grpc.WithTransportCredentials(insecure.NewCredentials()))
	if err != nil {
		log.Fatalf("Did not connect: %v", err)
	}
	return conn
}

func sendUrlToGrpc(url string, altText string) error {
	conn := setupConnection()
	defer func() {
		if err := conn.Close(); err != nil {
			log.Printf("Failed to close gRPC connection: %v", err)
		}
	}()

	client := pb.NewBackgroundImagesClient(conn)

	ctx, cancel := context.WithTimeout(context.Background(), persistTimeout)
	defer cancel()

	res, err := client.Persist(ctx, &pb.BackgroundImageDetails{
		Url:     url,
		AltText: altText,
		Source:  pb.ImageOfTheDaySource_Bing,
	})
	if err != nil {
		return err
	}

	log.Printf("Response: %v", res)
	return nil
}

func SendUrlToGrpc(url string, altText string) {
	if err := sendUrlToGrpc(url, altText); err != nil {
		log.Fatalf("Could not persist URL: %v", err)
	}
}
