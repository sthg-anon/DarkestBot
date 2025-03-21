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

using DarkestBot.Commands;
using DarkestBot.Commands.Payloads;
using DarkestBot.Model;
using Serilog;
using System.Text.Json;

namespace DarkestBot.MessageHandlers
{
    internal sealed class ChannelInviteHandler(JsonSerializerOptions jsonOptions, State state) : IMessageHandler
    {
        public async Task<Command?> HandleMessageAsync(string? payload, CancellationToken token = default)
        {
            if (payload == null)
            {
                Log.Error("Received a channel invite command with an empty payload.");
                return null;
            }

            ChannelInvitePayload? parsedPayload;
            try
            {
                parsedPayload = JsonSerializer.Deserialize<ChannelInvitePayload>(payload, jsonOptions);
            }
            catch (JsonException ex)
            {
                Log.Error(ex, "Unable to parse channel invite payload.");
                return null;
            }

            if (parsedPayload == null)
            {
                Log.Error("Channel invite payload parsed to null.");
                return null;
            }

            Log.Information("Received a channel invite to {channelName} by {character}", parsedPayload.Title, parsedPayload.Sender);

            state.RoomId = parsedPayload.Name;
            await state.SaveAsync(token);

            return new PayloadCommand<JoinChannelPayload>(MessageTypes.JCH, new JoinChannelPayload
            {
                Channel = parsedPayload.Name
            });
        }
    }
}
