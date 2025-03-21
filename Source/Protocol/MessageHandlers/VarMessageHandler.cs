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

using DarkestBot.Model;
using DarkestBot.Protocol.Commands;
using DarkestBot.Protocol.Commands.Payloads;
using Serilog;
using System.Text.Json;

namespace DarkestBot.Protocol.MessageHandlers
{
    internal sealed class VarMessageHandler(JsonSerializerOptions jsonOptions, State state) : IMessageHandler
    {
        private const string ChatMaxVar = "chat_max";
        private const string MessageDelayVar = "msg_flood";

        public Task<Command?> HandleMessageAsync(string? payload, CancellationToken token = default)
        {
            if (payload == null)
            {
                Log.Error("Received a var command with an empty payload.");
                return Task.FromResult<Command?>(null);
            }

            VarPayload? parsedPayload;
            try
            {
                parsedPayload = JsonSerializer.Deserialize<VarPayload>(payload, jsonOptions);
            }
            catch (JsonException ex)
            {
                Log.Error(ex, "Unable to parse var payload.");
                return Task.FromResult<Command?>(null);
            }

            if (parsedPayload == null)
            {
                Log.Error("Var payload parsed to null.");
                return Task.FromResult<Command?>(null);
            }

            if (string.IsNullOrEmpty(parsedPayload.Variable))
            {
                Log.Error("Got a var command with a null variable name.");
                return Task.FromResult<Command?>(null);
            }

            if (parsedPayload.Variable.Equals(ChatMaxVar))
            {
                if (parsedPayload.Value == null || !parsedPayload.Value.HasValue)
                {
                    Log.Warning("Variable {varName} has a null value.", ChatMaxVar);
                    return Task.FromResult<Command?>(null);
                }

                if (parsedPayload.Value.Value.TryGetInt32(out var maxChatBytes))
                {
                    state.MaxChatByteCount = maxChatBytes;
                    Log.Information("Max channel message length: {value}", state.MaxChatByteCount);
                }
                else
                {
                    Log.Warning("Unable to parse int for {varName}: {value}", parsedPayload.Variable, parsedPayload.Value);
                }
            }
            else if (parsedPayload.Variable.Equals(MessageDelayVar))
            {
                if (parsedPayload.Value == null || !parsedPayload.Value.HasValue)
                {
                    Log.Warning("Variable {varName} has a null value.", ChatMaxVar);
                    return Task.FromResult<Command?>(null);
                }

                if (parsedPayload.Value.Value.TryGetDouble(out var messageDelaySeconds))
                {
                    state.ChannelMessageDelay = messageDelaySeconds;
                    Log.Information("Message delay duration: {value}", state.ChannelMessageDelay);
                }
                else
                {
                    Log.Warning("Unable to parse double for {varName}: {value}", parsedPayload.Variable, parsedPayload.Value);
                }
            }

            return Task.FromResult<Command?>(null);
        }
    }
}
