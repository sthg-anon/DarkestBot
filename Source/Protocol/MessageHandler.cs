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
using DarkestBot.Protocol.MessageHandlers;
using Serilog;
using System.Text.Json;

namespace DarkestBot.Protocol
{
    internal sealed class MessageHandler
    {
        private const int MessageTypeLength = 3;
        private const int MinPayloadMessageLength = 4;

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private readonly Dictionary<MessageType, IMessageHandler> _messageHandlers;
        private readonly Dictionary<MessageType, IAsyncMessageHandler> _asyncMessageHandlers;

        private readonly HashSet<MessageType> _ignoredMessages =
        [
            MessageType.IDN, // character's own name on connect
            MessageType.FRL, // friends list
            MessageType.CON, // connection count
            MessageType.LIS, // list of all online characters
            MessageType.NLN, // person went online
            MessageType.FLN, // person went offline
            MessageType.STA, // person changed their status
            MessageType.COL, // channel op list
            MessageType.ICH, // initial channel data (user list, name, mode)
            MessageType.CDS, // Channel description change
            MessageType.HLO, // server version
            MessageType.IGN, // ignore list
            MessageType.ADL, // global op list
            MessageType.TPN, // typing status
            MessageType.JCH, // user joined channel
        ];

        public MessageHandler(State state, ICommandSender commandSender)
        {
            _messageHandlers = new()
            {
                { MessageType.PIN, new PingMessageHandler(commandSender) },
                { MessageType.VAR, new VarMessageHandler(_jsonOptions, state) }
            };

            _asyncMessageHandlers = new()
            {
                { MessageType.CIU, new ChannelInviteHandler(_jsonOptions, commandSender, state) },
                { MessageType.PRI, new PrivateMessageHandler(_jsonOptions, commandSender, state) },
                { MessageType.MSG, new ChannelMessageHandler(_jsonOptions, commandSender, state) }
            };
        }

        public async Task HandleMessageAsync(string message, CancellationToken token = default)
        {
            if (message.Length < MessageTypeLength)
            {
                Log.Warning("Received a message from F-Chat that is too short: {message}", message);
                return;
            }

            var messageType = MessageType.Get(message[..MessageTypeLength]);

            if (messageType == null)
            {
                Log.Information("Received unknown message (message type unknown): {message}", message);
                return;
            }
            else if (_ignoredMessages.Contains(messageType))
            {
                return;
            }

            string payload = string.Empty;
            if (message.Length >= MinPayloadMessageLength)
            {
                payload = message[MinPayloadMessageLength..];
            }

            if (_messageHandlers.TryGetValue(messageType, out var handler))
            {
                handler.HandleMessage(payload);
                return;
            }
            else if (_asyncMessageHandlers.TryGetValue(messageType, out var asyncHandler))
            {
                await asyncHandler.HandleMessageAsync(payload, token);
                return;
            }
            else
            {
                Log.Information("Message was not handled or ignored: {message}", message);
                return;
            }
        }
    }
}
