using BusinessLayer.API;

namespace ServiceLayer.ExternalServices;

public interface IJudge0Service
{
    Task<string> CreatePostQueryAsync(string[] inputs, string[] outputs, int languageId, string code, double timeLimit, double memoryLimit);
    Task<List<PostResponse>> SubmitBatchAsync(string request);
    Task<BatchSubmissionResponse> GetBatchResultsAsync(List<PostResponse> tokens);
}
