using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace RaceDay.Web.Services;

// The only class in this project that talks to the API
public class ApiService
{
    private readonly HttpClient _http;
    private readonly IHttpContextAccessor _accessor;
    private static readonly JsonSerializerOptions _json =
        new() { PropertyNameCaseInsensitive = true };

    public ApiService(HttpClient http, IHttpContextAccessor accessor)
    {
        _http = http;
        _accessor = accessor;
    }

    // Adds the login token (if the API gives one) to each request
    private void AddToken()
    {
        var token = _accessor.HttpContext?.Session.GetString("Token");
        _http.DefaultRequestHeaders.Authorization = string.IsNullOrEmpty(token)
            ? null
            : new AuthenticationHeaderValue("Bearer", token);
    }

    public async Task<T?> GetAsync<T>(string url)
    {
        AddToken();
        var res = await _http.GetAsync(url);
        if (!res.IsSuccessStatusCode) return default;
        var text = await res.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(text, _json);
    }

    public async Task<HttpResponseMessage> PostAsync(string url, object body)
    {
        AddToken();
        var content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
        return await _http.PostAsync(url, content);
    }

    public async Task<HttpResponseMessage> PutAsync(string url, object body)
    {
        AddToken();
        var content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
        return await _http.PutAsync(url, content);
    }

    public async Task<HttpResponseMessage> DeleteAsync(string url)
    {
        AddToken();
        return await _http.DeleteAsync(url);
    }

    // Sends an image to the API (the API puts it in storage, not this project)
    public async Task<HttpResponseMessage> UploadAsync(string url, IFormFile file)
    {
        AddToken();
        using var form = new MultipartFormDataContent();
        using var stream = file.OpenReadStream();
        form.Add(new StreamContent(stream), "file", file.FileName);
        return await _http.PostAsync(url, form);
    }
}
