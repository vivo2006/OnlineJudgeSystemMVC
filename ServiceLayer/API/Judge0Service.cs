using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using BusinessLayer.API;
using System.Xml;
using JsonSerializer = System.Text.Json.JsonSerializer;
using Formatting = Newtonsoft.Json.Formatting;

namespace ServiceLayer.ExternalServices
{
    public class Judge0Service : IJudge0Service
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly APIdata _apiData;

        public Judge0Service(IHttpClientFactory httpClientFactory, IOptions<APIdata> apiData)
        {
            _httpClientFactory = httpClientFactory;
            _apiData = apiData.Value;
        }

        public async Task<string> CreatePostQueryAsync(string[] inputTestFiles, string[] outputTestFiles, int languageId, string sourceCode, double timeLimit, double memoryLimit)
        {
            var batchRequest = new BatchSubmissions();
            if (inputTestFiles.Length == 0 || inputTestFiles.Length != outputTestFiles.Length)
                throw new ArgumentException("Matching test pairs are required.");
            int boundary = inputTestFiles.Length;

            for (int i = 0; i < boundary; i++)
            {
                string inputContent = await System.IO.File.ReadAllTextAsync(inputTestFiles[i]);
                string outputContent = await System.IO.File.ReadAllTextAsync(outputTestFiles[i]);

                batchRequest.submissions.Add(new PostRequest
                {
                    language_id = languageId,
                    source_code = sourceCode,
                    stdin = inputContent,
                    expected_output = outputContent,
                    cpu_time_limit = timeLimit,
                    memory_limit = memoryLimit
                });
            }

            return JsonConvert.SerializeObject(batchRequest, Formatting.Indented);
        }

        public async Task<List<PostResponse>> SubmitBatchAsync(string jsonRequest)
        {
            if (string.IsNullOrWhiteSpace(_apiData.Key)) throw new InvalidOperationException("Configure the Judge0 API key before submitting.");
            var client = _httpClientFactory.CreateClient();
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri("https://judge0-ce.p.rapidapi.com/submissions/batch?base64_encoded=false&wait=false"),
                Headers =
                {
                    { "x-rapidapi-key", _apiData.Key },
                    { "x-rapidapi-host", _apiData.Host },
                },
                Content = new StringContent(jsonRequest, Encoding.UTF8, "application/json"),
            };

            using var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<PostResponse>>(content) ?? throw new InvalidOperationException("Missing judge tokens.");
        }

        public async Task<BatchSubmissionResponse> GetBatchResultsAsync(List<PostResponse> tokens)
        {
            var client = _httpClientFactory.CreateClient();
            var tokenString = string.Join(",", tokens.Select(t => t.Token));

            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri($"https://judge0-ce.p.rapidapi.com/submissions/batch?tokens={tokenString}&base64_encoded=false&fields=stdout,status,time,memory"),
                Headers =
                {
                    { "x-rapidapi-key", _apiData.Key },
                    { "x-rapidapi-host", _apiData.Host },
                },
            };

            using var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<BatchSubmissionResponse>(content) ?? throw new InvalidOperationException("Missing judge results.");
        }
    }
}
