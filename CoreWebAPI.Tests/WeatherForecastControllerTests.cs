using System;
using System.Linq;
using CoreWebAPI.Controllers;
using FluentAssertions;
using Xunit;

namespace CoreWebAPI.Tests;

public class WeatherForecastControllerTests
{
    private readonly WeatherForecastController _controller = new();

    [Fact]
    public void Get_ReturnsFiveForecasts()
    {
        var result = _controller.Get();

        result.Should().HaveCount(5);
    }

    [Fact]
    public void Get_ReturnsForecastsWithFutureDates()
    {
        var result = _controller.Get().ToList();
        var today = DateOnly.FromDateTime(DateTime.Now);

        result.Should().OnlyContain(f => f.Date > today);
    }

    [Fact]
    public void Get_ReturnsForecastsWithNonNullSummary()
    {
        var result = _controller.Get();

        result.Should().OnlyContain(f => f.Summary != null);
    }
}
