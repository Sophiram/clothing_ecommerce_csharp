using System.Net.Http.Json;
using System.Text.Json;
using ClothingEcommerce.Shared.Common;

namespace ClothingEcommerce.Client.Services.ApiClient
{
    public interface IApiClient
    {
        Task<ApiResponse<T>?> GetAsync<T>(string endpoint);
        Task<ApiResponse<TResponse>?> PostAsync<TRequest, TResponse>(string endpoint, TRequest data);
        Task<ApiResponse?> PostAsync<TRequest>(string endpoint, TRequest data);
        Task<ApiResponse?> PostMultipartAsync(string endpoint, MultipartFormDataContent content);
        Task<ApiResponse<TResponse>?> PutAsync<TRequest, TResponse>(string endpoint, TRequest data);
        Task<ApiResponse?> PutAsync<TRequest>(string endpoint, TRequest data);
        Task<ApiResponse<TResponse>?> DeleteAsync<TResponse>(string endpoint);
        Task<ApiResponse?> DeleteAsync(string endpoint);
    }

    public class ApiClient : IApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ApiClient> _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        public ApiClient(HttpClient httpClient, ILogger<ApiClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }

        public async Task<ApiResponse<T>?> GetAsync<T>(string endpoint)
        {
            try
            {
                var response = await _httpClient.GetAsync(endpoint);
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<ApiResponse<T>>(_jsonOptions);
                }

                var error = await response.Content.ReadFromJsonAsync<ApiResponse<T>>(_jsonOptions);
                return error ?? ApiResponse<T>.Fail($"Server responded with {(int)response.StatusCode}: {response.ReasonPhrase}", (int)response.StatusCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "HTTP GET failed for endpoint {Endpoint}", endpoint);
                return ApiResponse<T>.Fail($"Network error communicating with API: {ex.Message}", 500);
            }
        }

        public async Task<ApiResponse<TResponse>?> PostAsync<TRequest, TResponse>(string endpoint, TRequest data)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync(endpoint, data, _jsonOptions);
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<ApiResponse<TResponse>>(_jsonOptions);
                }

                var error = await response.Content.ReadFromJsonAsync<ApiResponse<TResponse>>(_jsonOptions);
                return error ?? ApiResponse<TResponse>.Fail($"Server responded with {(int)response.StatusCode}: {response.ReasonPhrase}", (int)response.StatusCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "HTTP POST failed for endpoint {Endpoint}", endpoint);
                return ApiResponse<TResponse>.Fail($"Network error communicating with API: {ex.Message}", 500);
            }
        }

        public async Task<ApiResponse?> PostAsync<TRequest>(string endpoint, TRequest data)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync(endpoint, data, _jsonOptions);
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<ApiResponse>(_jsonOptions);
                }

                var error = await response.Content.ReadFromJsonAsync<ApiResponse>(_jsonOptions);
                return error ?? ApiResponse.Fail($"Server responded with {(int)response.StatusCode}: {response.ReasonPhrase}", (int)response.StatusCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "HTTP POST failed for endpoint {Endpoint}", endpoint);
                return ApiResponse.Fail($"Network error communicating with API: {ex.Message}", 500);
            }
        }

        public async Task<ApiResponse?> PostMultipartAsync(string endpoint, MultipartFormDataContent content)
        {
            try
            {
                var response = await _httpClient.PostAsync(endpoint, content);
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<ApiResponse>(_jsonOptions);
                }

                var error = await response.Content.ReadFromJsonAsync<ApiResponse>(_jsonOptions);
                return error ?? ApiResponse.Fail($"Server responded with {(int)response.StatusCode}: {response.ReasonPhrase}", (int)response.StatusCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "HTTP multipart POST failed for endpoint {Endpoint}", endpoint);
                return ApiResponse.Fail($"Network error communicating with API: {ex.Message}", 500);
            }
        }

        public async Task<ApiResponse?> PutAsync<TRequest>(string endpoint, TRequest data)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync(endpoint, data, _jsonOptions);
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<ApiResponse>(_jsonOptions);
                }

                var error = await response.Content.ReadFromJsonAsync<ApiResponse>(_jsonOptions);
                return error ?? ApiResponse.Fail($"Server responded with {(int)response.StatusCode}: {response.ReasonPhrase}", (int)response.StatusCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "HTTP PUT failed for endpoint {Endpoint}", endpoint);
                return ApiResponse.Fail($"Network error communicating with API: {ex.Message}", 500);
            }
        }

        public async Task<ApiResponse<TResponse>?> PutAsync<TRequest, TResponse>(string endpoint, TRequest data)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync(endpoint, data, _jsonOptions);
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<ApiResponse<TResponse>>(_jsonOptions);
                }

                var error = await response.Content.ReadFromJsonAsync<ApiResponse<TResponse>>(_jsonOptions);
                return error ?? ApiResponse<TResponse>.Fail($"Server responded with {(int)response.StatusCode}: {response.ReasonPhrase}", (int)response.StatusCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "HTTP PUT failed for endpoint {Endpoint}", endpoint);
                return ApiResponse<TResponse>.Fail($"Network error communicating with API: {ex.Message}", 500);
            }
        }

        public async Task<ApiResponse<TResponse>?> DeleteAsync<TResponse>(string endpoint)
        {
            try
            {
                var response = await _httpClient.DeleteAsync(endpoint);
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<ApiResponse<TResponse>>(_jsonOptions);
                }

                var error = await response.Content.ReadFromJsonAsync<ApiResponse<TResponse>>(_jsonOptions);
                return error ?? ApiResponse<TResponse>.Fail($"Server responded with {(int)response.StatusCode}: {response.ReasonPhrase}", (int)response.StatusCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "HTTP DELETE failed for endpoint {Endpoint}", endpoint);
                return ApiResponse<TResponse>.Fail($"Network error communicating with API: {ex.Message}", 500);
            }
        }

        public async Task<ApiResponse?> DeleteAsync(string endpoint)
        {
            try
            {
                var response = await _httpClient.DeleteAsync(endpoint);
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<ApiResponse>(_jsonOptions);
                }

                var error = await response.Content.ReadFromJsonAsync<ApiResponse>(_jsonOptions);
                return error ?? ApiResponse.Fail($"Server responded with {(int)response.StatusCode}: {response.ReasonPhrase}", (int)response.StatusCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "HTTP DELETE failed for endpoint {Endpoint}", endpoint);
                return ApiResponse.Fail($"Network error communicating with API: {ex.Message}", 500);
            }
        }
    }
}
