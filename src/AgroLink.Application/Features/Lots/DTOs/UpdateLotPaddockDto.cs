using System.ComponentModel.DataAnnotations;

namespace AgroLink.Application.Features.Lots.DTOs;

public class UpdateLotPaddockDto
{
    [Required]
    public int PaddockId { get; set; }
}
