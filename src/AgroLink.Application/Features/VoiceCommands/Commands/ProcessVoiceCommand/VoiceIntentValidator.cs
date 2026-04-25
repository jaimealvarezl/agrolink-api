using AgroLink.Application.Features.VoiceCommands.DTOs;

namespace AgroLink.Application.Features.VoiceCommands.Commands.ProcessVoiceCommand;

public static class VoiceIntentValidator
{
    public static ParsedIntentResponse Validate(ParsedIntentResponse intent, FarmRosterDto roster)
    {
        var validAnimalIds = new HashSet<int>(roster.Animals.Select(a => a.Id));
        var validLotIds = new HashSet<int>(roster.Lots.Select(l => l.Id));
        var validPaddockIds = new HashSet<int>(roster.Lots.Select(l => l.PaddockId));

        var confidence = intent.Confidence;
        var animalId = intent.AnimalId;
        var lotId = intent.LotId;
        var targetPaddockId = intent.TargetPaddockId;
        var motherId = intent.MotherId;

        if (animalId.HasValue && !validAnimalIds.Contains(animalId.Value))
        {
            animalId = null;
            confidence -= 0.2;
        }

        if (motherId.HasValue && !validAnimalIds.Contains(motherId.Value))
        {
            motherId = null;
            confidence -= 0.2;
        }

        if (lotId.HasValue && !validLotIds.Contains(lotId.Value))
        {
            lotId = null;
            confidence -= 0.2;
        }

        if (targetPaddockId.HasValue && !validPaddockIds.Contains(targetPaddockId.Value))
        {
            targetPaddockId = null;
            confidence -= 0.2;
        }

        if (Math.Round(confidence, 4) < 0.5)
        {
            return new ParsedIntentResponse(
                "unknown",
                0.0,
                null,
                null,
                null,
                null,
                null,
                null,
                null
            );
        }

        return intent with
        {
            Confidence = confidence,
            AnimalId = animalId,
            LotId = lotId,
            TargetPaddockId = targetPaddockId,
            MotherId = motherId,
        };
    }
}
