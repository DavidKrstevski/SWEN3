using System.Net.Http.Headers;
using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using Paperless_API.Entities;

namespace Paperless_API.Tests
{
    [TestFixture]
    public class DocumentIntegrationTests
    {
        private HttpClient _client = null!;
        private const string BaseUrl = "http://localhost:8080/api/documents";

        [SetUp]
        public void SetUp()
        {
            _client = new HttpClient();
        }

        [TearDown]
        public void TearDown()
        {
            _client.Dispose();
        }

        [Test]
        public async Task UploadDocument_ShouldPersistAndBeRetrievable()
        {
            var content = new MultipartFormDataContent();
            var fileBytes = File.ReadAllBytes("TestFiles/HelloWorld.pdf");
            var fileContent = new ByteArrayContent(fileBytes);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
            content.Add(fileContent, "file", "HelloWorld.pdf");

            var response = await _client.PostAsync(BaseUrl, content);
            response.EnsureSuccessStatusCode();

            var responseString = await response.Content.ReadAsStringAsync();
            var uploadedDoc = JsonSerializer.Deserialize<Document>(responseString, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            uploadedDoc.Should().NotBeNull();
            uploadedDoc!.FileName.Should().Be("HelloWorld.pdf");
            uploadedDoc.Size.Should().Be(fileBytes.Length);

            var getResponse = await _client.GetAsync($"{BaseUrl}/{uploadedDoc.Id}");
            getResponse.EnsureSuccessStatusCode();

            var getDoc = JsonSerializer.Deserialize<Document>(await getResponse.Content.ReadAsStringAsync(),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            getDoc.Should().NotBeNull();
            getDoc!.Id.Should().Be(uploadedDoc.Id);
            getDoc.FileName.Should().Be("HelloWorld.pdf");

            var deleteResponse = await _client.DeleteAsync($"{BaseUrl}/{uploadedDoc.Id}");
            deleteResponse.EnsureSuccessStatusCode();
        }
    }
}
