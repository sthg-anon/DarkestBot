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

using DarkestBot.MessageHandlers;
using DarkestBot.Model;
using Serilog;
using System.Text.Json;

namespace DarkestBot
{
    internal sealed class MessageHandler
    {
        private const int MessageTypeLength = 3;
        private const int MinPayloadMessageLength = 4;

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private readonly Dictionary<string, IMessageHandler> _messageHandlers;

        private readonly HashSet<string> _ignoredMessages =
        [
            MessageTypes.IDN, // character's own name on connect
            "FRL", // friends list
            "CON", // connection count
            "LIS", // list of all online characters
            "NLN", // person went online
            "FLN", // person went offline
            "STA", // person changed their status
            "COL", // channel op list
            "ICH", // initial channel data (user list, name, mode)
            "CDS", // Channel description change
            "HLO", // server version
            "IGN", // ignore list
            "ADL", // global op list
            "TPN", // typing status
            "PRI", // PRIVATE MESSAGES
            MessageTypes.JCH, // user joined channel
        ];

        public MessageHandler(State state)
        {
            _messageHandlers = new()
            {
                { MessageTypes.PIN, new PingMessageHandler() },
                { MessageTypes.CIU, new ChannelInviteHandler(_jsonOptions, state) },
                { MessageTypes.VAR, new VarMessageHandler(_jsonOptions, state) },
                { MessageTypes.MSG, new ChannelMessageHandler(_jsonOptions, state) }
            };
        }

        public async Task<string?> HandleMessageAsync(string message, CancellationToken token = default)
        {
            if (message.Length < MessageTypeLength)
            {
                Log.Warning("Received a message from F-Chat that is too short: {message}", message);
                return null;
            }

            var messageType = message[..MessageTypeLength];

            if (_messageHandlers.TryGetValue(messageType, out var handler))
            {
                string payload = string.Empty;
                if (message.Length >= MinPayloadMessageLength)
                {
                    payload = message[MinPayloadMessageLength..];
                }

                var response = await handler.HandleMessageAsync(payload, token);
                if (response == null)
                {
                    return null;
                }

                return response.MakeFChatCommand();
            }
            else if (_ignoredMessages.Contains(messageType))
            {
                return null;
            }
            else
            {
                Log.Information("Received unknown message: {message}", message);
                return null;
            }
        }
    }
}
