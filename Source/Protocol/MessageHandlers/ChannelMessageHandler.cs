﻿/*
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
using DarkestBot.Protocol.Commands.Payloads;
using DarkestBot.UserCommands;
using Serilog;
using System.Text.Json;

namespace DarkestBot.Protocol.MessageHandlers
{
    internal sealed class ChannelMessageHandler(JsonSerializerOptions jsonOptions, ICommandSender commandSender, StateManager stateManager) : IAsyncMessageHandler
    {
        private readonly UserCommandHandler _commandHandler = new(jsonOptions, stateManager, commandSender, UserCommandMode.Public);

        public async Task HandleMessageAsync(string? payload, CancellationToken token = default)
        {
            if (!PayloadParser.TryParsePayload<ChannelMessagePayload>(jsonOptions, payload, out var parsedPayload))
            {
                return;
            }

            if (!parsedPayload.Channel?.Equals(stateManager.State.RoomId) ?? false)
            {
                Log.Warning(
                    "Received a message from a strange channel ({channel}) from {sender}: {message}",
                    parsedPayload.Channel,
                    parsedPayload.Character,
                    parsedPayload.Message);
                return;
            }

            if (string.IsNullOrEmpty(parsedPayload.Channel))
            {
                Log.Warning("Received a message from a null channel: {message}", payload);
                return;
            }

            if (string.IsNullOrEmpty(parsedPayload.Message))
            {
                Log.Warning("Received a null channel message: {message}", payload);
                return;
            }

            if (string.IsNullOrEmpty(parsedPayload.Character))
            {
                Log.Warning("Received a channel message from a null character: {message}", payload);
                return;
            }

            var responder = new ChannelChatResponder(commandSender, stateManager, parsedPayload.Channel);
            await _commandHandler.HandleCommandAsync(parsedPayload.Character, parsedPayload.Message, responder, token);
        }
    }
}
