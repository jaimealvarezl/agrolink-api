using AgroLink.Application.Features.VoiceCommands.DTOs;

namespace AgroLink.Application.Features.ExternalWorkers.Models;

public record ExtractVoiceIntentPayload(string Transcript, FarmRosterDto Roster);
