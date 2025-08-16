using NUnit.Framework;
using RestSharp;
using RestSharp.Authenticators;
using System.Net;
using System.Text.Json;




namespace StorySpoiler
{
    [TestFixture]
    public class StoryTest
    {
        private RestClient client;
        private static string createStoryId;
        private const string baseUrl = "https://d3s5nxhwblsjbi.cloudfront.net";

        [OneTimeSetUp]
        public void Setup()
        {
            string token = GetJwtToken("TonyTest11", "TonyTest11");

            var option = new RestClientOptions(baseUrl)
            {
                Authenticator = new JwtAuthenticator(token)
            };
            client = new RestClient(option);
        }

        private string GetJwtToken(string username, string password)
        {
            var loginClient = new RestClient(baseUrl);
            var request = new RestRequest("api/User/Authentication", Method.Post);

            request.AddJsonBody(new {username, password});

            var response = loginClient.Execute(request);

            var json = JsonSerializer.Deserialize<JsonElement>(response.Content);

            return json.GetProperty("accessToken").GetString() ?? string.Empty;
        }

        [Test, Order(1)]
        public void CreateStory_ShouldReturnCreated()
        {
            var story = new
            {
                title = "New story2",
                description = "Test story description"
            };

            var request = new RestRequest("/api/Story/Create", Method.Post);
            request.AddJsonBody(story);

            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));

            var json = JsonSerializer.Deserialize<JsonElement>(response.Content);
            createStoryId = json.GetProperty("storyId").GetString();
        }

        [Test, Order(2)]
        public void EditStory_ShouldReturnOk()
        {
            var updatedStory = new
            {
                title = "Updated Story Title",
                description = "Updated Story Description",
                url = ""
            };

            var request = new RestRequest($"/api/Story/Edit/{createStoryId}", Method.Put);
            request.AddJsonBody(updatedStory);

            var response = client.Execute(request);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        }

        [Test, Order(3)]
        public void GetAllStories_ShouldReturnOk()
        {
            var request = new RestRequest("/api/Story/All", Method.Get);

            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            var json = JsonSerializer.Deserialize<JsonElement>(response.Content!);
            Assert.That(json.ValueKind, Is.EqualTo(JsonValueKind.Array), "Response should be an array");
            Assert.That(json.GetArrayLength(), Is.GreaterThan(0), "Array should not be empty");
        }

        [Test, Order(4)]
        public void DeleteStory_ShouldReturnOk()
        {
            var request = new RestRequest($"/api/Story/Delete/{createStoryId}", Method.Delete);
            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        }

        [Test, Order(5)]
        public void CreateStory_WithoutRequiredFields_ShouldReturnBadRequest()
        {
            var incompleteStory = new
            {
                url = "" // missing title and description
            };

            var request = new RestRequest("/api/Story/Create", Method.Post);
            request.AddJsonBody(incompleteStory);

            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }
        [Test, Order(6)]
        public void EditNonExistingStory_ShouldReturnNotFound()
        {
            var fakeStoryId = "4444";
            var updatedStory = new
            {
                title = "Updated Story",
                description = "Updated Description",
                url = ""
            };

            var request = new RestRequest($"/api/Story/Edit/{fakeStoryId}", Method.Put);
            request.AddJsonBody(updatedStory);

            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));

            if (!string.IsNullOrEmpty(response.Content))
            {
                Assert.That(response.Content, Does.Contain("No spoilers"));
            }
        }
        [Test, Order(7)]
        public void DeleteNonExistingStory_ShouldReturnBadRequest()
        {
            var fakeStoryId = "non-existing-id"; 
            var request = new RestRequest($"/api/Story/Delete/{fakeStoryId}", Method.Delete);

            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

            
            if (!string.IsNullOrEmpty(response.Content))
            {
                Assert.That(response.Content, Does.Contain("Unable to delete this story spoiler!"));
            }
        }


        [OneTimeTearDown]
        public void Cleanup()
        {
            client?.Dispose();
        }
    }
}