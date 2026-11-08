// João Victor Cardoso Gonçalves - 155953 - DEV02

using System;

namespace AcademiaDoZe.Domain.Exceptions;

public sealed class DomainException(string message) : Exception(message)
{
}