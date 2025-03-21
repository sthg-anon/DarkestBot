/*
 * Copyright (c) 2025 Aller
 * 
 * This software is provided 'as-is', without any express or implied
 * warranty. In no event will the authors be held liable for any damages
 * arising from the use of this software.
 * 
 * Permission is granted to anyone to use this software for any purpose,
 * including commercial applications, and to alter it and redistribute it
 * freely, subject to the following restrictions:
 * 
 * 1. The origin of this software must not be misrepresented; you must not
 *    claim that you wrote the original software. If you use this software
 *    in a product, an acknowledgment in the product documentation would be
 *    appreciated but is not required.
 * 2. Altered source versions must be plainly marked as such, and must not be
 *    misrepresented as being the original software.
 * 3. This notice may not be removed or altered from any source distribution.
 */

using Polly;
using Polly.Contrib.WaitAndRetry;
using Serilog;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace DarkestBot.Login
{
    internal sealed class TicketFactory
    {
        private const int InternalServerErrorRangeStart = 500;
        private const int RetryDurationMs = 300;
        private const int RetryCount = 3;

#if DEBUG
        private const int TicketExpireTimeMinutes = 28;
#endif

        private const int STD_INPUT_HANDLE = -10;
        private const int ENABLE_ECHO_INPUT = 0x0004;

        [DllImport("kernel32.dll")]
        static extern bool SetConsoleMode(nint hConsoleHandle, int mode);

        [DllImport("kernel32.dll")]
        static extern bool GetConsoleMode(nint hConsoleHandle, out int mode);

        [DllImport("kernel32.dll")]
        static extern nint GetStdHandle(int handle);

        record Credentials(string Username, string Password);

        private static readonly HttpClient _client = new()
        {
            BaseAddress = new Uri("https://www.f-list.net/json/")
        };

        private static readonly JsonSerializerOptions _jsonParseOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private readonly string _ticketCacheFilePath;

#if DEBUG
        private TicketCacheFile? _ticketCache = null;
#endif

        public TicketFactory(string cacheFilePath = "ticket.json")
        {
            if (string.IsNullOrWhiteSpace(cacheFilePath))
            {
                throw new ArgumentException("Cache file path cannot be null or empty.", nameof(cacheFilePath));
            }

            _ticketCacheFilePath = cacheFilePath;
        }

        private readonly AsyncPolicy<HttpResponseMessage> _retryPolicy = Policy
            .Handle<HttpRequestException>()
            .OrResult<HttpResponseMessage>(r => (int)r.StatusCode >= InternalServerErrorRangeStart)
            .WaitAndRetryAsync(Backoff.DecorrelatedJitterBackoffV2(TimeSpan.FromMilliseconds(RetryDurationMs), RetryCount),
            (result, timespan, retryCount, context) =>
            {
                Log.Warning(
                    "Get Ticket retry {count} after {time}ms due to {error}",
                    retryCount,
                    timespan.TotalMilliseconds,
                    result.Exception?.Message ?? result.Result.StatusCode.ToString());
            });

        public async Task<Ticket?> GetTicketAsync(CancellationToken token = default)
        {
            try
            {
#if DEBUG
                _ticketCache ??= await LoadTicketCacheFileAsync(token) ?? new TicketCacheFile();

                if (_ticketCache.ExpirationTime.HasValue && _ticketCache.ExpirationTime > DateTime.UtcNow)
                {
                    Log.Information("Using cached ticket.");

                    if (string.IsNullOrWhiteSpace(_ticketCache.Account))
                    {
                        Log.Error("Ticket cache had a null/empty account!");
                    }
                    else if (string.IsNullOrWhiteSpace(_ticketCache.Ticket))
                    {
                        Log.Error("Ticket cache had a null/empty ticket!");
                    }
                    else
                    {
                        return new Ticket(_ticketCache.Account, _ticketCache.Ticket);
                    }
                }

                DeleteTicketCache();
#endif

                Log.Information("Retreiving new ticket.");
                var credentials = GetCredentials();
                if (credentials == null)
                {
                    Log.Error("Unable to read credentials.");
                    return null;
                }

                var response = await _retryPolicy.ExecuteAsync(async () =>
                {
                    var content = new FormUrlEncodedContent(
                    new Dictionary<string, string>
                    {
                        { "account", credentials.Username },
                        { "password", credentials.Password },
                        { "no_characters", "true" },
                        { "no_friends", "true" },
                        { "no_bookmarks", "true" }
                    });
                    using var request = new HttpRequestMessage(HttpMethod.Post, "getApiTicket.php") { Content = content };
                    return await _client.SendAsync(request, token);
                });
                response.EnsureSuccessStatusCode();

                var responseText = await response.Content.ReadAsStringAsync(token);
                var parsedResponse = JsonSerializer.Deserialize<TicketResponse>(responseText, _jsonParseOptions)
                    ?? throw new TicketException("Parsed response was null.");

                if (!string.IsNullOrEmpty(parsedResponse.Error))
                {
                    throw new TicketException($"Ticket request failed: {parsedResponse.Error}");
                }

                var ticket = parsedResponse.Ticket ?? throw new TicketException("Ticket field was empty in ticket response.");


#if DEBUG
                _ticketCache.Ticket = ticket;
                _ticketCache.Account = credentials.Username;
                _ticketCache.ExpirationTime = DateTime.UtcNow.AddMinutes(TicketExpireTimeMinutes);
                await SaveTicketCacheFileAsync(_ticketCache, token);
#endif

                return new Ticket(credentials.Username, ticket);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (HttpRequestException ex)
            {
                throw new TicketException("HTTP request failed while fetching ticket.", ex);
            }
            catch (JsonException ex)
            {
                throw new TicketException("HTTP reseponse had unfamiliar json.", ex);
            }
            catch (Exception ex)
            {
                throw new TicketException("Unexpected error occurred while getting ticket.", ex);
            }
        }

#if DEBUG
        private void DeleteTicketCache()
        {
            try
            {
                File.Delete(_ticketCacheFilePath);
            }
            catch (UnauthorizedAccessException ex)
            {
                Log.Error(ex, $"Permissions issues deleting ticket cache file at {_ticketCacheFilePath}");
            }
            catch (IOException ex)
            {
                Log.Error(ex, $"Error deleting ticket cache file at {_ticketCacheFilePath}");
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Unknown exception while deleting ticket cache file at {_ticketCacheFilePath}");
            }
        }

        private async Task SaveTicketCacheFileAsync(TicketCacheFile cacheFile, CancellationToken token = default)
        {
            try
            {
                var json = JsonSerializer.Serialize(cacheFile);

                await File.WriteAllTextAsync(_ticketCacheFilePath, json, token);
                Log.Information("Ticket cached to file: {path}", _ticketCacheFilePath);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unable to save ticket cache file.");
            }
        }

        private async Task<TicketCacheFile?> LoadTicketCacheFileAsync(CancellationToken token = default)
        {
            try
            {
                if (!File.Exists(_ticketCacheFilePath))
                {
                    Log.Information("Ticket cache at {path} does not exist (yet).", _ticketCacheFilePath);
                    return null;
                }

                var text = await File.ReadAllTextAsync(_ticketCacheFilePath, token);
                var ticketCache = JsonSerializer.Deserialize<TicketCacheFile>(text, _jsonParseOptions);

                if (ticketCache == null)
                {
                    Log.Warning("Read a null ticket cache from {file}", _ticketCacheFilePath);
                    DeleteTicketCache();
                    return null;
                }

                return ticketCache;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (UnauthorizedAccessException ex)
            {
                Log.Error(ex, $"Permissions issues reading ticket cache file at {_ticketCacheFilePath}");
                return null;
            }
            catch (IOException ex)
            {
                Log.Error(ex, $"Error reading ticket cache file at {_ticketCacheFilePath}");
                return null;
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Unknown exception while reading ticket cache file at {_ticketCacheFilePath}");
                return null;
            }
        }
#endif

        static string? ReadPassword()
        {
            nint handle = GetStdHandle(STD_INPUT_HANDLE);
            GetConsoleMode(handle, out int mode);
            SetConsoleMode(handle, mode & ~ENABLE_ECHO_INPUT); // Disable input echo

            string? password = Console.ReadLine();

            SetConsoleMode(handle, mode); // Restore input echo
            return password;
        }

        private static Credentials? GetCredentials()
        {
            Console.Write("User name: ");
            var username = Console.ReadLine();
            if (string.IsNullOrEmpty(username))
            {
                Log.Warning("Null/empty username given.");
                return null;
            }

            Console.Write("Password: ");
            var password = ReadPassword();
            if (string.IsNullOrEmpty(password))
            {
                Log.Warning("Null/empty password given.");
                return null;
            }

            return new Credentials(username, password);
        }
    }
}
