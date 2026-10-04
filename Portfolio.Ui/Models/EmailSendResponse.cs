using Portfolio.Grpc;

namespace Portfolio.Ui.Models;

public sealed record EmailSendResponse(bool Success, ResultCode ResultCode);
