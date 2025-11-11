using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;
using System.Net.Sockets;

namespace Server.Middlewares;

public class RequestClientInfoMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestClientInfoMiddleware> _logger;
    private readonly IMemoryCache _cache;

    public RequestClientInfoMiddleware(RequestDelegate next, ILogger<RequestClientInfoMiddleware> logger, IMemoryCache cache)
    {
        _next = next;
        _logger = logger;
        _cache = cache;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var request = context.Request;

        // 1. L?y IP (ýu tiên X-Forwarded-For n?u có)
        string? ipString = null;
        if (request.Headers.TryGetValue("X-Forwarded-For", out var xff))
        {
            // X-Forwarded-For có th? ch?a nhi?u IP: client, proxy1, proxy2...
            var first = xff.ToString().Split(',')[0].Trim();
            ipString = first;
        }
        if (string.IsNullOrEmpty(ipString))
        {
            ipString = request.HttpContext.Connection.RemoteIpAddress?.ToString();
        }
        if (string.IsNullOrEmpty(ipString))
        {
            _logger.LogWarning("Không th? xác ð?nh IP client.");
            context.Items["ClientIp"] = "unknown";
            await _next(context);
            return;
        }

        // 2. N?u cached th? dùng cache
        if (_cache.TryGetValue<string?>($"host_{ipString}", out var cachedHostname))
        {
            Attach(context, ipString, cachedHostname);
            await _next(context);
            return;
        }

        // 3. Reverse DNS lookup with timeout và x? l? an toàn
        string? hostname = null;
        try
        {
            if (IPAddress.TryParse(ipString, out var ipAddr))
            {
                // Chu?n hóa ð? log d? ð?c (map IPv6 -> IPv4 n?u c?n)
                var printableIp = (ipAddr.AddressFamily == AddressFamily.InterNetworkV6
                    ? ipAddr.MapToIPv4()
                    : ipAddr).ToString();
                ipString = printableIp;

                var lookupTask = Dns.GetHostEntryAsync(ipAddr);
                var timeout = Task.Delay(TimeSpan.FromSeconds(3)); // 3s timeout
                var finished = await Task.WhenAny(lookupTask, timeout);
                if (finished == lookupTask)
                {
                    var entry = lookupTask.Result;
                    hostname = entry.HostName;
                }
                else
                {
                    _logger.LogWarning("Reverse DNS lookup timeout for IP {ip}", ipString);
                }
            }
            else
            {
                _logger.LogWarning("IP {ip} không parse ðý?c.", ipString);
            }
        }
        catch (SocketException sx)
        {
            _logger.LogDebug(sx, "Reverse DNS failed for {ip}", ipString);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during reverse DNS for {ip}", ipString);
        }

        // 4. Cache k?t qu? (k? c? null) ð? tránh lookup l?p l?i
        _cache.Set($"host_{ipString}", hostname, TimeSpan.FromMinutes(30));

        Attach(context, ipString, hostname);

        await _next(context);
    }

    private static void Attach(HttpContext context, string ip, string? host)
    {
        // Lýu vào HttpContext.Items
        context.Items["ClientIp"] = ip;
        if (!string.IsNullOrWhiteSpace(host))
            context.Items["ClientHost"] = host;

        // Thêm response headers ð? ti?n debug/quan sát
        context.Response.OnStarting(() =>
        {
            if (!string.IsNullOrEmpty(ip))
                context.Response.Headers["X-Client-IP"] = ip;
            if (!string.IsNullOrEmpty(host))
                context.Response.Headers["X-Client-Host"] = host!;
            return Task.CompletedTask;
        });
    }
}

public static class RequestClientInfoMiddlewareExtensions
{
    public static IApplicationBuilder UseRequestClientInfo(this IApplicationBuilder app)
        => app.UseMiddleware<RequestClientInfoMiddleware>();
}
