using System;

namespace AcademiaDoZe.Domain.Exceptions;

public sealed class DomainException(string message) : Exception(message)
{
}