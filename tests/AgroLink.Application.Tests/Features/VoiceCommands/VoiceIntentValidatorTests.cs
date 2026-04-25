using AgroLink.Application.Features.VoiceCommands.Commands.ProcessVoiceCommand;
using AgroLink.Application.Features.VoiceCommands.DTOs;
using Shouldly;

namespace AgroLink.Application.Tests.Features.VoiceCommands;

[TestFixture]
public class VoiceIntentValidatorTests
{
    private static FarmRosterDto BuildRoster(
        int[]? animalIds = null,
        int[]? lotIds = null,
        int[]? paddockIds = null
    )
    {
        var animals = (animalIds ?? [1, 2, 3])
            .Select(id => new AnimalRosterEntry(id, $"Animal {id}", null, null, 1, "Lote Norte"))
            .ToList();

        var lots = (lotIds ?? [10, 11])
            .Zip(
                paddockIds ?? [100, 101],
                (lid, pid) => new LotRosterEntry(lid, $"Lote {lid}", pid, $"Potrero {pid}")
            )
            .ToList();

        return new FarmRosterDto(animals, lots);
    }

    [Test]
    public void Validate_AllValidIds_ConfidenceUnchanged()
    {
        var roster = BuildRoster();
        var intent = new ParsedIntentResponse("move_animal", 0.9, 1, 10);

        var result = VoiceIntentValidator.Validate(intent, roster);

        result.Intent.ShouldBe("move_animal");
        result.Confidence.ShouldBe(0.9);
        result.AnimalId.ShouldBe(1);
        result.LotId.ShouldBe(10);
    }

    [Test]
    public void Validate_InvalidAnimalId_NulledAndConfidencePenalized()
    {
        var roster = BuildRoster();
        var intent = new ParsedIntentResponse("move_animal", 0.9, 999, 10);

        var result = VoiceIntentValidator.Validate(intent, roster);

        result.AnimalId.ShouldBeNull();
        result.Confidence.ShouldBe(0.7, 0.001);
        result.Intent.ShouldBe("move_animal");
    }

    [Test]
    public void Validate_InvalidLotId_NulledAndConfidencePenalized()
    {
        var roster = BuildRoster();
        var intent = new ParsedIntentResponse("move_animal", 0.9, 1, 999);

        var result = VoiceIntentValidator.Validate(intent, roster);

        result.LotId.ShouldBeNull();
        result.Confidence.ShouldBe(0.7, 0.001);
    }

    [Test]
    public void Validate_InvalidPaddockId_NulledAndConfidencePenalized()
    {
        var roster = BuildRoster();
        var intent = new ParsedIntentResponse("move_lot", 0.8, null, 10, 999);

        var result = VoiceIntentValidator.Validate(intent, roster);

        result.TargetPaddockId.ShouldBeNull();
        result.Confidence.ShouldBe(0.6, 0.001);
    }

    [Test]
    public void Validate_InvalidMotherId_NulledAndConfidencePenalized()
    {
        var roster = BuildRoster();
        var intent = new ParsedIntentResponse("register_newborn", 0.85, null, null, null, 999, "F");

        var result = VoiceIntentValidator.Validate(intent, roster);

        result.MotherId.ShouldBeNull();
        result.Confidence.ShouldBe(0.65, 0.001);
    }

    [Test]
    public void Validate_MultipleInvalidIds_ConfidencePenalizedPerInvalid()
    {
        var roster = BuildRoster();
        var intent = new ParsedIntentResponse("move_animal", 0.9, 999, 999);

        var result = VoiceIntentValidator.Validate(intent, roster);

        result.AnimalId.ShouldBeNull();
        result.LotId.ShouldBeNull();
        result.Confidence.ShouldBe(0.5, 0.001);
        result.Intent.ShouldBe("move_animal");
    }

    [Test]
    public void Validate_ConfidenceBelowThreshold_DowngradedToUnknown()
    {
        var roster = BuildRoster();
        // 3 invalid IDs from 0.9 → 0.9 - 0.6 = 0.3 < 0.5
        var intent = new ParsedIntentResponse("move_animal", 0.9, 999, 999, 999);

        var result = VoiceIntentValidator.Validate(intent, roster);

        result.Intent.ShouldBe("unknown");
        result.Confidence.ShouldBe(0.0);
        result.AnimalId.ShouldBeNull();
        result.LotId.ShouldBeNull();
        result.TargetPaddockId.ShouldBeNull();
    }

    [Test]
    public void Validate_EmptyRoster_EntityIdsPenalized()
    {
        var emptyRoster = new FarmRosterDto([], []);
        // 0.8 - 0.2 (animal) - 0.2 (lot) = 0.4 < 0.5 → downgrade
        var intent = new ParsedIntentResponse("move_animal", 0.8, 1, 10);

        var result = VoiceIntentValidator.Validate(intent, emptyRoster);

        result.AnimalId.ShouldBeNull();
        result.LotId.ShouldBeNull();
        result.Intent.ShouldBe("unknown");
        result.Confidence.ShouldBe(0.0);
    }

    [Test]
    public void Validate_NullEntityIds_NoConfidencePenalty()
    {
        var roster = BuildRoster();
        var intent = new ParsedIntentResponse();

        var result = VoiceIntentValidator.Validate(intent, roster);

        result.Intent.ShouldBe("unknown");
        result.Confidence.ShouldBe(0.0);
    }

    [Test]
    public void Validate_ExactlyAtThreshold_NotDowngraded()
    {
        var roster = BuildRoster();
        // One invalid ID from 0.7 → 0.5, which is NOT < 0.5
        var intent = new ParsedIntentResponse(
            "create_note",
            0.7,
            999,
            null,
            null,
            null,
            null,
            "some note"
        );

        var result = VoiceIntentValidator.Validate(intent, roster);

        result.Intent.ShouldBe("create_note");
        result.Confidence.ShouldBe(0.5, 0.001);
        result.AnimalId.ShouldBeNull();
    }

    [Test]
    public void Validate_CreateAnimal_ValidLotId_PassesThrough()
    {
        var roster = BuildRoster();
        var intent = new ParsedIntentResponse(
            "create_animal",
            0.91,
            null,
            10,
            null,
            null,
            "female",
            null,
            "la milagro",
            "017683344",
            "colorada",
            "2020-05-22",
            ["Carla", "Jaime"]
        );

        var result = VoiceIntentValidator.Validate(intent, roster);

        result.Intent.ShouldBe("create_animal");
        result.Confidence.ShouldBe(0.91);
        result.LotId.ShouldBe(10);
        result.AnimalName.ShouldBe("la milagro");
        result.EarTag.ShouldBe("017683344");
        result.Color.ShouldBe("colorada");
        result.BirthDate.ShouldBe("2020-05-22");
        result.OwnerNames.ShouldBe(["Carla", "Jaime"]);
    }

    [Test]
    public void Validate_CreateAnimal_InvalidLotId_PenalizesConfidence()
    {
        var roster = BuildRoster();
        var intent = new ParsedIntentResponse(
            "create_animal",
            0.9,
            null,
            999,
            null,
            null,
            "female",
            null,
            "la milagro",
            "017683344"
        );

        var result = VoiceIntentValidator.Validate(intent, roster);

        result.LotId.ShouldBeNull();
        result.Confidence.ShouldBe(0.7, 0.001);
        result.AnimalName.ShouldBe("la milagro");
    }

    [Test]
    public void Validate_RegisterNewborn_ColorAndBirthDatePassThrough()
    {
        var roster = BuildRoster();
        var intent = new ParsedIntentResponse(
            "register_newborn",
            0.88,
            null,
            null,
            null,
            1,
            "male",
            null,
            null,
            null,
            "colorado",
            "2024-05-22"
        );

        var result = VoiceIntentValidator.Validate(intent, roster);

        result.Intent.ShouldBe("register_newborn");
        result.MotherId.ShouldBe(1);
        result.Color.ShouldBe("colorado");
        result.BirthDate.ShouldBe("2024-05-22");
    }

    [Test]
    public void Validate_ValidPaddockIdFromLots_NotPenalized()
    {
        var roster = BuildRoster(paddockIds: [100, 101]);
        var intent = new ParsedIntentResponse("move_lot", 0.85, null, 10, 100);

        var result = VoiceIntentValidator.Validate(intent, roster);

        result.TargetPaddockId.ShouldBe(100);
        result.Confidence.ShouldBe(0.85);
    }
}
