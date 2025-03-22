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

using Serilog;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DarkestBot.Model
{
    internal sealed class State
    {
        private const string StateFilePath = "state.json";
        private const int DefaultMaxChatByteCount = 4096;

        private static readonly JsonSerializerOptions _jsonParseOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        [JsonPropertyName("RoomId")]
        public string? RoomId { get; set; }

        [JsonIgnore]
        public int MaxChatByteCount { get; set; } = DefaultMaxChatByteCount;

        [JsonIgnore]
        public double ChannelMessageDelay { get; set; } = 0.0;

        [JsonPropertyName("Characters")]
        public Dictionary<string, Character> Characters { get; set; } = [];

        public async Task SaveAsync(CancellationToken token = default)
        {
            try
            {
                var json = JsonSerializer.Serialize(this);
                await File.WriteAllTextAsync(StateFilePath, json, token);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unable to save state file.");
            }
        }

        public static async Task<State?> LoadAsync(CancellationToken token = default)
        {
            try
            {
                if (!File.Exists(StateFilePath))
                {
                    Log.Information("State file at {path} does not exist (yet).", StateFilePath);
                    var newState = new State();
                    newState.Characters.Add("Aller the Fox", new Character { IsOp = true });
                    newState.Characters.Add("Rouge SexBat", new Character { IsOp = true });
                    newState.Characters.Add("MilkyBun", new Character { IsOp = true });
                    return newState;
                }

                var text = await File.ReadAllTextAsync(StateFilePath, token);
                var state = JsonSerializer.Deserialize<State>(text, _jsonParseOptions);

                if (state == null)
                {
                    Log.Warning("Read a null state file from {file}", StateFilePath);
                    return null;
                }

                return state;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (UnauthorizedAccessException ex)
            {
                Log.Error(ex, $"Permissions issues reading state file at {StateFilePath}");
                return null;
            }
            catch (IOException ex)
            {
                Log.Error(ex, $"Error reading state file at {StateFilePath}");
                return null;
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Unknown exception while reading state file at {StateFilePath}");
                return null;
            }
        }
    }
}
