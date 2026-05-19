using System;

namespace CoreMVC.SharedKernel;

public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
}
