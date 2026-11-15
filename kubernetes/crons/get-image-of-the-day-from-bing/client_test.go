package main

import (
	"context"
	"net"
	"testing"
	"time"

	pb "get-image-of-the-day-from-bing/proto"
	"google.golang.org/grpc"
	"google.golang.org/grpc/codes"
	"google.golang.org/grpc/stats"
	"google.golang.org/grpc/status"
	"google.golang.org/protobuf/types/known/emptypb"
)

type connCloseTracker struct {
	closed chan struct{}
}

func (t *connCloseTracker) TagRPC(ctx context.Context, _ *stats.RPCTagInfo) context.Context {
	return ctx
}

func (t *connCloseTracker) HandleRPC(context.Context, stats.RPCStats) {}

func (t *connCloseTracker) TagConn(ctx context.Context, _ *stats.ConnTagInfo) context.Context {
	return ctx
}

func (t *connCloseTracker) HandleConn(_ context.Context, s stats.ConnStats) {
	if _, ok := s.(*stats.ConnEnd); ok {
		select {
		case t.closed <- struct{}{}:
		default:
		}
	}
}

type fakeBackgroundImagesServer struct {
	pb.UnimplementedBackgroundImagesServer
	errToReturn error
	gotDetails  *pb.BackgroundImageDetails
	gotDeadline time.Duration
}

func (s *fakeBackgroundImagesServer) Persist(ctx context.Context, req *pb.BackgroundImageDetails) (*emptypb.Empty, error) {
	if deadline, ok := ctx.Deadline(); ok {
		s.gotDeadline = time.Until(deadline)
	}
	s.gotDetails = req
	if s.errToReturn != nil {
		return nil, s.errToReturn
	}
	return &emptypb.Empty{}, nil
}

func TestPersistTimeoutConstant(t *testing.T) {
	if persistTimeout != 10*time.Second {
		t.Fatalf("expected persistTimeout to be 10s, got %v", persistTimeout)
	}
}

func TestSendUrlToGrpc_DeadlineAndClosesConnection(t *testing.T) {
	tests := []struct {
		name      string
		serverErr error
		wantErr   bool
	}{
		{
			name:      "success closes connection and sets 10s deadline",
			serverErr: nil,
			wantErr:   false,
		},
		{
			name:      "rpc error still closes connection",
			serverErr: status.Error(codes.Internal, "db unavailable"),
			wantErr:   true,
		},
	}

	for _, tt := range tests {
		t.Run(tt.name, func(t *testing.T) {
			tracker := &connCloseTracker{closed: make(chan struct{}, 1)}
			fakeSrv := &fakeBackgroundImagesServer{errToReturn: tt.serverErr}

			lis, err := net.Listen("tcp", "127.0.0.1:0")
			if err != nil {
				t.Fatalf("failed to listen: %v", err)
			}
			srv := grpc.NewServer(grpc.StatsHandler(tracker))
			pb.RegisterBackgroundImagesServer(srv, fakeSrv)
			go func() {
				_ = srv.Serve(lis)
			}()
			defer srv.Stop()

			t.Setenv("GRPC_BASE_URL", lis.Addr().String())

			err = sendUrlToGrpc("https://example.com/bing.jpg", "Test Alt")
			if (err != nil) != tt.wantErr {
				t.Fatalf("sendUrlToGrpc() error = %v, wantErr %v", err, tt.wantErr)
			}

			if fakeSrv.gotDeadline <= 5*time.Second || fakeSrv.gotDeadline > 10*time.Second {
				t.Fatalf("expected remaining deadline in (5s, 10s], got %v", fakeSrv.gotDeadline)
			}

			if !tt.wantErr && (fakeSrv.gotDetails == nil || fakeSrv.gotDetails.Url != "https://example.com/bing.jpg" || fakeSrv.gotDetails.Source != pb.ImageOfTheDaySource_Bing) {
				t.Fatalf("unexpected persisted details: %+v", fakeSrv.gotDetails)
			}

			select {
			case <-tracker.closed:
			case <-time.After(2 * time.Second):
				t.Fatal("expected gRPC connection to be closed after sendUrlToGrpc returned")
			}
		})
	}
}
