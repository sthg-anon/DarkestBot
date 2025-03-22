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

using DarkestBot.Protocol.Commands.Payloads;
using DarkestBot.Protocol.Commands;

namespace DarkestBot.Protocol
{
    internal static class CommandFactory
    {
        public static PayloadCommand<ChannelMessagePayload> ChannelMessage(string? channel, string? message) =>
            new(MessageType.MSG, new ChannelMessagePayload
            {
                Channel = channel,
                Message = message
            });
        
        public static PayloadCommand<JoinChannelPayload> JoinChannel(string? channel) =>
            new(MessageType.JCH, new JoinChannelPayload
            {
                Channel = channel
            });

        public static PayloadCommand<IdentityPayload> Identify(
            string? method,
            string? account,
            string? ticket,
            string? character,
            string? clientName,
            string? clientVersion) =>
            new(MessageType.IDN, new IdentityPayload
            {
                Method = method,
                Account = account,
                Ticket = ticket,
                Character = character,
                ClientName = clientName,
                ClientVersion = clientVersion
            });

        public static Command Ping() => new(MessageType.PIN);

        public static PayloadCommand<PrivateMessagePayload> PrivateMessage(string character, string message) =>
            new(MessageType.PRI, new PrivateMessagePayload
            {
                Recipient = character,
                Message = message
            });
    }
}
