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
using DarkestBot.Payloads;
using System.Text.Json;

namespace DarkestBot
{
    internal sealed class BotCommandHandler(JsonSerializerOptions jsonOptions, State state)
    {
        private const string DataDumpCommand = "!datadump";

        public Task<Command?> HandleCommandAsync(string character, string message, CancellationToken token = default)
        {
            if (message.StartsWith(DataDumpCommand))
            {
                return HandleDataDumpAsync(character);
            }

            return Task.FromResult<Command?>(null);
        }

        private Task<Command?> HandleDataDumpAsync(string character)
        {
            if(state.Characters.TryGetValue(character, out var data))
            {
                var dump = JsonSerializer.Serialize(data, jsonOptions);
                return Task.FromResult<Command?>(new PayloadCommand<ChannelMessagePayload>(MessageTypes.MSG, new ChannelMessagePayload
                {
                    Channel = state.RoomId,
                    Message = dump
                }));
            }
            else
            {
                return Task.FromResult<Command?>(new PayloadCommand<ChannelMessagePayload>(MessageTypes.MSG, new ChannelMessagePayload
                {
                    Channel = state.RoomId,
                    Message = $"No data found for [user]{character}[/user]"
                }));
            }
        }
    }
}
