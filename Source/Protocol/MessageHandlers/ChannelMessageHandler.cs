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
    internal sealed class ChannelMessageHandler(JsonSerializerOptions jsonOptions, State state) : IMessageHandler
    {
        private readonly BotCommandHandler _commandHandler = new(jsonOptions, state);

        public Task<Command?> HandleMessageAsync(string? payload, CancellationToken token = default)
        {
            if (payload == null)
            {
                Log.Error("Received a channel message with an empty payload.");
                return Task.FromResult<Command?>(null);
            }

            ChannelMessagePayload? parsedPayload;
            try
            {
                parsedPayload = JsonSerializer.Deserialize<ChannelMessagePayload>(payload, jsonOptions);
            }
            catch (JsonException ex)
            {
                Log.Error(ex, "Unable to parse channel message payload.");
                return Task.FromResult<Command?>(null);
            }

            if (parsedPayload == null)
            {
                Log.Error("Channel message payload parsed to null.");
                return Task.FromResult<Command?>(null);
            }

            if (!parsedPayload.Channel?.Equals(state.RoomId) ?? false)
            {
                Log.Warning(
                    "Received a message from a strange channel ({channel}) from {sender}: {message}",
                    parsedPayload.Channel,
                    parsedPayload.Character,
                    parsedPayload.Message);
                return Task.FromResult<Command?>(null);
            }

            if (string.IsNullOrEmpty(parsedPayload.Channel))
            {
                Log.Warning("Received a message from a null channel: {message}", payload);
                return Task.FromResult<Command?>(null);
            }

            if (string.IsNullOrEmpty(parsedPayload.Message))
            {
                Log.Warning("Received a null channel message: {message}", payload);
                return Task.FromResult<Command?>(null);
            }

            if (string.IsNullOrEmpty(parsedPayload.Character))
            {
                Log.Warning("Received a channel message from a null character: {message}", payload);
                return Task.FromResult<Command?>(null);
            }

            return _commandHandler.HandleCommandAsync(parsedPayload.Character, parsedPayload.Message, token);
        }
    }
}
