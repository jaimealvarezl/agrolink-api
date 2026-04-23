using System.Globalization;
using AgroLink.Application.Interfaces;
using AgroLink.Domain.Enums;
using AgroLink.Domain.Interfaces;
using MediatR;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace AgroLink.Application.Features.Animals.Queries.GetTheftAlertPdf;

public class GetTheftAlertPdfQueryHandler(
    IAnimalRepository animalRepository,
    IStorageService storageService,
    ICurrentUserService currentUserService
) : IRequestHandler<GetTheftAlertPdfQuery, byte[]?>
{
    private const string OrangeHex = "#FF6B00";
    private const string DarkHex = "#1A1A1A";
    private const string MutedHex = "#6B7280";
    private const string SurfaceHex = "#F9F5EF";
    private const string BorderHex = "#E5DDD0";

    public async Task<byte[]?> Handle(
        GetTheftAlertPdfQuery request,
        CancellationToken cancellationToken
    )
    {
        var animal = await animalRepository.GetAnimalDetailsAsync(request.AnimalId, request.UserId);

        if (animal == null)
        {
            return null;
        }

        if (
            currentUserService.CurrentFarmId.HasValue
            && animal.Lot?.Paddock?.FarmId != currentUserService.CurrentFarmId.Value
        )
        {
            return null;
        }

        var primaryPhotoKey = animal
            .Photos.OrderByDescending(p => p.IsProfile)
            .ThenByDescending(p => p.UploadedAt)
            .FirstOrDefault()
            ?.StorageKey;

        byte[]? photoBytes = null;
        if (primaryPhotoKey != null)
        {
            photoBytes = await storageService.GetFileBytesAsync(primaryPhotoKey, cancellationToken);
        }

        var now = DateTime.UtcNow;
        var ageInMonths =
            (now.Year - animal.BirthDate.Year) * 12 + now.Month - animal.BirthDate.Month;
        if (now.Day < animal.BirthDate.Day)
        {
            ageInMonths--;
        }

        return Document
            .Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A5);
                    page.Margin(0);
                    page.DefaultTextStyle(x => x.FontFamily("Helvetica").FontColor(DarkHex));

                    page.Content()
                        .Column(col =>
                        {
                            col.Item()
                                .Background(OrangeHex)
                                .Padding(16)
                                .Row(row =>
                                {
                                    row.RelativeItem()
                                        .AlignMiddle()
                                        .Text("⚠  ALERTA DE ROBO")
                                        .FontSize(18)
                                        .Bold()
                                        .FontColor("#FFFFFF");

                                    row.ConstantItem(70)
                                        .AlignMiddle()
                                        .AlignRight()
                                        .Text("AgroLink")
                                        .FontSize(10)
                                        .FontColor("#FFD4A8");
                                });

                            col.Item()
                                .Background(SurfaceHex)
                                .Padding(16)
                                .Row(row =>
                                {
                                    var photoCell = row.ConstantItem(80);
                                    if (photoBytes != null)
                                    {
                                        photoCell.Width(80).Height(80).Image(photoBytes).FitArea();
                                    }
                                    else
                                    {
                                        var initial = (animal.Name ?? animal.TagVisual ?? "?")[..1]
                                            .ToUpperInvariant();
                                        photoCell
                                            .Width(80)
                                            .Height(80)
                                            .Background("#2D6A4F")
                                            .AlignCenter()
                                            .AlignMiddle()
                                            .Text(initial)
                                            .FontSize(36)
                                            .Bold()
                                            .FontColor("#FFFFFF");
                                    }

                                    row.RelativeItem()
                                        .PaddingLeft(14)
                                        .Column(info =>
                                        {
                                            info.Item()
                                                .Text(animal.Name ?? animal.TagVisual ?? "—")
                                                .FontSize(17)
                                                .Bold();

                                            if (!string.IsNullOrWhiteSpace(animal.TagVisual))
                                            {
                                                info.Item()
                                                    .PaddingTop(4)
                                                    .Text($"Arete: {animal.TagVisual}")
                                                    .FontSize(12)
                                                    .FontColor(MutedHex);
                                            }

                                            if (!string.IsNullOrWhiteSpace(animal.Cuia))
                                            {
                                                info.Item()
                                                    .PaddingTop(2)
                                                    .Text($"CUIA: {animal.Cuia}")
                                                    .FontSize(12)
                                                    .FontColor(MutedHex);
                                            }

                                            info.Item()
                                                .PaddingTop(8)
                                                .Background(OrangeHex)
                                                .PaddingHorizontal(8)
                                                .PaddingVertical(3)
                                                .AlignLeft()
                                                .Text("ROBADO")
                                                .FontSize(9)
                                                .Bold()
                                                .FontColor("#FFFFFF");
                                        });
                                });

                            col.Item().Height(1).Background(BorderHex);

                            col.Item()
                                .Background("#FFFFFF")
                                .Padding(16)
                                .Column(details =>
                                {
                                    details
                                        .Item()
                                        .Row(row =>
                                        {
                                            FieldCell(
                                                row.RelativeItem(),
                                                "Raza",
                                                animal.Breed ?? "—"
                                            );
                                            FieldCell(
                                                row.RelativeItem(),
                                                "Sexo",
                                                animal.Sex == Sex.Female ? "Hembra" : "Macho"
                                            );
                                        });

                                    details
                                        .Item()
                                        .PaddingTop(10)
                                        .Row(row =>
                                        {
                                            FieldCell(
                                                row.RelativeItem(),
                                                "Color",
                                                animal.Color ?? "—"
                                            );
                                            FieldCell(
                                                row.RelativeItem(),
                                                "Edad",
                                                FormatAge(ageInMonths)
                                            );
                                        });

                                    details
                                        .Item()
                                        .PaddingTop(10)
                                        .Row(row =>
                                        {
                                            FieldCell(
                                                row.RelativeItem(),
                                                "Último lote",
                                                animal.Lot?.Name ?? "—"
                                            );
                                        });
                                });

                            col.Item().Height(1).Background(BorderHex);

                            if (animal.AnimalOwners.Count > 0)
                            {
                                col.Item()
                                    .Background(SurfaceHex)
                                    .Padding(16)
                                    .Column(owners =>
                                    {
                                        owners
                                            .Item()
                                            .PaddingBottom(6)
                                            .Text("PROPIETARIOS")
                                            .FontSize(9)
                                            .Bold()
                                            .FontColor(OrangeHex);

                                        foreach (var owner in animal.AnimalOwners)
                                        {
                                            owners
                                                .Item()
                                                .PaddingVertical(2)
                                                .Row(row =>
                                                {
                                                    row.RelativeItem()
                                                        .Text(owner.Owner?.Name ?? "—")
                                                        .FontSize(11);

                                                    row.ConstantItem(50)
                                                        .AlignRight()
                                                        .Text($"{owner.SharePercent:F0}%")
                                                        .FontSize(11)
                                                        .Bold()
                                                        .FontColor(OrangeHex);
                                                });
                                        }
                                    });

                                col.Item().Height(1).Background(BorderHex);
                            }

                            col.Item()
                                .Background("#FFFFFF")
                                .Padding(16)
                                .Text("Por favor reportar avistamientos a las autoridades locales.")
                                .FontSize(11)
                                .Italic()
                                .FontColor(MutedHex);

                            col.Item()
                                .Background(OrangeHex)
                                .Padding(10)
                                .Row(row =>
                                {
                                    row.RelativeItem()
                                        .AlignMiddle()
                                        .Text(
                                            $"Generado: {now.ToString("dd MMMM yyyy", new CultureInfo("es-MX"))}"
                                        )
                                        .FontSize(9)
                                        .FontColor("#FFD4A8");

                                    row.ConstantItem(70)
                                        .AlignMiddle()
                                        .AlignRight()
                                        .Text("agrolink.mx")
                                        .FontSize(9)
                                        .FontColor("#FFD4A8");
                                });
                        });
                });
            })
            .GeneratePdf();
    }

    private static void FieldCell(IContainer cell, string label, string value)
    {
        cell.Column(col =>
        {
            col.Item().Text(label).FontSize(8).FontColor("#9CA3AF");
            col.Item().PaddingTop(1).Text(value).FontSize(12).Bold();
        });
    }

    private static string FormatAge(int months)
    {
        if (months <= 0)
        {
            return "< 1 mes";
        }

        var years = months / 12;
        var rem = months % 12;

        return (years, rem) switch
        {
            (0, var m) => $"{m} mes{(m != 1 ? "es" : "")}",
            (var y, 0) => $"{y} año{(y != 1 ? "s" : "")}",
            var (y, m) => $"{y} año{(y != 1 ? "s" : "")} y {m} mes{(m != 1 ? "es" : "")}",
        };
    }
}
