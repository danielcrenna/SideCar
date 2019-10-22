using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MyBlazorApp.Models;
using SideCar.Blazor;

namespace MyBlazorApp.Server.Controllers
{
    [Route("api/SampleData")]
    public class SampleDataController : Controller
    {
        private readonly IOptionsSnapshot<SideCarOptions> _options;

        public SampleDataController(IOptionsSnapshot<SideCarOptions> options)
        {
            _options = options;
        }

        private static string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        [HttpGet(nameof(WeatherForecasts))]
        public IEnumerable<WeatherForecast> WeatherForecasts()
        {
            var rng = new Random();
            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateTime.Now.AddDays(index),
                TemperatureC = rng.Next(-20, 55),
                Summary = Summaries[rng.Next(Summaries.Length)]
            });
        }

        [HttpGet(nameof(GetRunMode))]
        public RunMode GetRunMode()
        {
            return new RunMode { Mode = _options.Value.RunAt.ToString() };
        }
    }
}
