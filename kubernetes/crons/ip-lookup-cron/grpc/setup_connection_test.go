package grpc

import (
	"context"
	"net"
	"testing"
	"time"

	pb "ip-lookup-cron/proto"
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

type fakeVisitorInsightsServer struct {
	pb.UnimplementedVisitorInsightsServer
	ipsToReturn     []*pb.IpCheckDto
	errToReturn     error
	gotCheckedIps   []*pb.IpCheckDto
	gotLastDeadline time.Duration
}

func (s *fakeVisitorInsightsServer) GetIpsToCheck(ctx context.Context, _ *emptypb.Empty) (*pb.GetIpsToCheckResponse, error) {
	if deadline, ok := ctx.Deadline(); ok {
		s.gotLastDeadline = time.Until(deadline)
	}
	if s.errToReturn != nil {
		return nil, s.errToReturn
	}
	return &pb.GetIpsToCheckResponse{Ips: s.ipsToReturn}, nil
}

func (s *fakeVisitorInsightsServer) PersistCheckedIps(ctx context.Context, req *pb.PersistCheckedIpsRequest) (*emptypb.Empty, error) {
	if deadline, ok := ctx.Deadline(); ok {
		s.gotLastDeadline = time.Until(deadline)
	}
	s.gotCheckedIps = req.CheckedIps
	if s.errToReturn != nil {
		return nil, s.errToReturn
	}
	return &emptypb.Empty{}, nil
}

func startTestServer(t *testing.T, fakeSrv *fakeVisitorInsightsServer) (*connCloseTracker, string) {
	t.Helper()

	tracker := &connCloseTracker{closed: make(chan struct{}, 2)}
	lis, err := net.Listen("tcp", "127.0.0.1:0")
	if err != nil {
		t.Fatalf("failed to listen: %v", err)
	}

	srv := grpc.NewServer(grpc.StatsHandler(tracker))
	pb.RegisterVisitorInsightsServer(srv, fakeSrv)
	go func() {
		_ = srv.Serve(lis)
	}()
	t.Cleanup(srv.Stop)

	return tracker, lis.Addr().String()
}

func assertConnClosed(t *testing.T, tracker *connCloseTracker) {
	t.Helper()
	select {
	case <-tracker.closed:
	case <-time.After(2 * time.Second):
		t.Fatal("expected gRPC connection to be closed after RPC completed")
	}
}

func TestGetIpsToCheck_ClosesConnection(t *testing.T) {
	tests := []struct {
		name      string
		serverErr error
		wantErr   bool
	}{
		{name: "success closes connection", serverErr: nil, wantErr: false},
		{name: "error still closes connection", serverErr: status.Error(codes.Unavailable, "down"), wantErr: true},
	}

	for _, tt := range tests {
		t.Run(tt.name, func(t *testing.T) {
			fakeSrv := &fakeVisitorInsightsServer{
				ipsToReturn: []*pb.IpCheckDto{{IpAddress: "8.8.8.8"}},
				errToReturn: tt.serverErr,
			}
			tracker, addr := startTestServer(t, fakeSrv)
			t.Setenv("GRPC_BASE_URL", addr)

			ips, err := getIpsToCheck()
			if (err != nil) != tt.wantErr {
				t.Fatalf("getIpsToCheck() error = %v, wantErr %v", err, tt.wantErr)
			}
			if !tt.wantErr && (len(ips) != 1 || ips[0].IpAddress != "8.8.8.8") {
				t.Fatalf("unexpected IPs returned: %+v", ips)
			}
			if fakeSrv.gotLastDeadline <= 10*time.Second || fakeSrv.gotLastDeadline > grpcTimeout {
				t.Fatalf("expected deadline in (10s, %v], got %v", grpcTimeout, fakeSrv.gotLastDeadline)
			}
			assertConnClosed(t, tracker)
		})
	}
}

func TestSendIpCheckResults_ClosesConnection(t *testing.T) {
	tests := []struct {
		name      string
		serverErr error
		wantErr   bool
	}{
		{name: "success closes connection", serverErr: nil, wantErr: false},
		{name: "error still closes connection", serverErr: status.Error(codes.Internal, "write failed"), wantErr: true},
	}

	for _, tt := range tests {
		t.Run(tt.name, func(t *testing.T) {
			fakeSrv := &fakeVisitorInsightsServer{errToReturn: tt.serverErr}
			tracker, addr := startTestServer(t, fakeSrv)
			t.Setenv("GRPC_BASE_URL", addr)

			input := []*pb.IpCheckDto{{IpAddress: "1.1.1.1", Operation: pb.DbOperationForThisRow_UPDATE}}
			err := sendIpCheckResults(input)
			if (err != nil) != tt.wantErr {
				t.Fatalf("sendIpCheckResults() error = %v, wantErr %v", err, tt.wantErr)
			}
			if len(fakeSrv.gotCheckedIps) != 1 || fakeSrv.gotCheckedIps[0].IpAddress != "1.1.1.1" {
				t.Fatalf("unexpected checked IPs sent: %+v", fakeSrv.gotCheckedIps)
			}
			if fakeSrv.gotLastDeadline <= 10*time.Second || fakeSrv.gotLastDeadline > grpcTimeout {
				t.Fatalf("expected deadline in (10s, %v], got %v", grpcTimeout, fakeSrv.gotLastDeadline)
			}
			assertConnClosed(t, tracker)
		})
	}
}
