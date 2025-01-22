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

func setupConnection() *grpc.ClientConn {
	host, _ := os.LookupEnv("GRPC_SERVER_HOST")
	port, _ := os.LookupEnv("GRPC_SERVER_PORT")

	conn, err := grpc.NewClient(host+":"+port, grpc.WithTransportCredentials(insecure.NewCredentials()))
	if err != nil {
		log.Fatalf("Did not connect: %v", err)
	}
	return conn
}

func SendUrlToGrpc(url string) {

	conn := setupConnection()
	//client := pb.NewBingPersistImageServiceClient(conn)
	client := pb.NewBackgroundImagesClient(conn)

	ctx, cancel := context.WithTimeout(context.Background(), time.Second)
	defer cancel()

	res, err := client.Persist(ctx, &pb.Url{Url: url})
	if err != nil {
		log.Fatalf("Could not persist URL: %v", err)
	}

	log.Printf("Response: %v", res)
}
