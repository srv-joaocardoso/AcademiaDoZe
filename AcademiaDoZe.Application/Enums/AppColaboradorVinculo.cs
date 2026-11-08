// João Victor Cardoso Gonçalves - 155953 - DEV02

using System.ComponentModel.DataAnnotations;

namespace AcademiaDoZe.Application.Enums;

public enum AppColaboradorVinculo
{
    [Display(Name = "CLT")]
    CLT = 0,

    [Display(Name = "Estagiário")]
    Estagio = 1
}
