using System.Net;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace CoreMVC.Web.IntegrationTests
{
    public class HomePageTests : IClassFixture<WebApplicationFactory<CoreMVC.Web.Program>>
    {
        private readonly WebApplicationFactory<CoreMVC.Web.Program> _factory;

        public HomePageTests(WebApplicationFactory<CoreMVC.Web.Program> factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task GetRoot_ReturnsSuccessAndContainsTitle()
        {
            var client = _factory.CreateClient();

            var response = await client.GetAsync("/");

            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain("<title");
        }
    }
}
