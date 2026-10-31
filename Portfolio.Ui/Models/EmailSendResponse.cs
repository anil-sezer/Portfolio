namespace Portfolio.Ui.Models;

public enum EmailSendResultCode
{
    Success = 0,
    Error = 1,
    Forbidden = 2
}

public sealed record EmailSendResponse(bool Success, EmailSendResultCode ResultCode);
