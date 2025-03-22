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
using DarkestBot.Protocol.Commands.Payloads;
using Serilog;
using System.Text.Json;

namespace DarkestBot.Protocol.MessageHandlers
{
    internal sealed class VarMessageHandler(JsonSerializerOptions jsonOptions, StateManager stateManager) : IMessageHandler
    {
        private const string ChatMaxVar = "chat_max";
        private const string MessageDelayVar = "msg_flood";

        public void HandleMessage(string? payload)
        {
            if (!PayloadParser.TryParsePayload<VarPayload>(jsonOptions, payload, out var parsedPayload))
            {
                return;
            }

            if (string.IsNullOrEmpty(parsedPayload.Variable))
            {
                Log.Error("Got a var command with a null variable name.");
                return;
            }

            if (parsedPayload.Variable.Equals(ChatMaxVar))
            {
                if (parsedPayload.Value == null || !parsedPayload.Value.HasValue)
                {
                    Log.Warning("Variable {varName} has a null value.", ChatMaxVar);
                    return;
                }

                if (parsedPayload.Value.Value.TryGetInt32(out var maxChatBytes))
                {
                    stateManager.TransientState.MaxChatByteCount = maxChatBytes;
                    Log.Information("Max channel message length: {value}", stateManager.TransientState.MaxChatByteCount);
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
                    return;
                }

                if (parsedPayload.Value.Value.TryGetDouble(out var messageDelaySeconds))
                {
                    stateManager.TransientState.ChannelMessageDelay = messageDelaySeconds;
                    Log.Information("Message delay duration: {value}", stateManager.TransientState.ChannelMessageDelay);
                }
                else
                {
                    Log.Warning("Unable to parse double for {varName}: {value}", parsedPayload.Variable, parsedPayload.Value);
                }
            }

            return;
        }
    }
}
