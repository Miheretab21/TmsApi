namespace TmsApi.Application.Hubs;

public interface ITmsHubClient
{
    // New: broadcast enrollment status changes to all connected clients
    Task ReceiveEnrollmentStatusUpdated(string enrollmentId, string status);
}
