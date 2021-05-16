using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Xunit;

namespace Demo.Tests
{
    public class WeatherForecastTests
    {
        [Fact]
        public void Missing_summary_fails_validation()
        {
            var forecast = new WeatherForecastV1();
            var context = new ValidationContext(forecast);
            var results = new List<ValidationResult>();
            var valid = Validator.TryValidateObject(forecast, context, results, true);
            
            Assert.False(valid);
            Assert.Single(results);
            Assert.Equal("The Summary field is required.", results[0].ErrorMessage);
        }

        [Fact]
        public void Invalid_summary_fails_validation()
        {
            var forecast = new WeatherForecastV1 {Summary = "Crunchy"};
            var context = new ValidationContext(forecast);
            var results = new List<ValidationResult>();
            var valid = Validator.TryValidateObject(forecast, context, results, true);

            Assert.False(valid);
            Assert.Single(results);
            Assert.Equal("Crunchy is not one of: Balmy, Bracing, Chilly, Cool, Freezing, Hot, Mild, Scorching, Sweltering, Warm", results[0].ErrorMessage);
        }
    }
}
