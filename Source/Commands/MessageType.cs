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

namespace DarkestBot.Commands
{
    internal sealed class MessageType
    {
        // This needs to be at the top! Moving it around tends to break the MessageType constructor because
        // _registry will be null.
        private static readonly Dictionary<string, MessageType> _registry = new(StringComparer.Ordinal);

        // A
        public static readonly MessageType ADL = new("ADL");

        // C
        public static readonly MessageType CDS = new("CDS");
        public static readonly MessageType CIU = new("CIU");
        public static readonly MessageType COL = new("COL");
        public static readonly MessageType CON = new("CON");

        // F
        public static readonly MessageType FLN = new("FLN");
        public static readonly MessageType FRL = new("FRL");

        // H
        public static readonly MessageType HLO = new("HLO");

        // I
        public static readonly MessageType ICH = new("ICH");
        public static readonly MessageType IDN = new("IDN");
        public static readonly MessageType IGN = new("IGN");

        // J
        public static readonly MessageType JCH = new("JCH");

        // L
        public static readonly MessageType LIS = new("LIS");

        // M
        public static readonly MessageType MSG = new("MSG");

        // N
        public static readonly MessageType NLN = new("NLN");

        // P
        public static readonly MessageType PIN = new("PIN");
        public static readonly MessageType PRI = new("PRI");

        // S
        public static readonly MessageType STA = new("STA");

        // T
        public static readonly MessageType TPN = new("TPN");

        // V
        public static readonly MessageType VAR = new("VAR");

        public string Code { get; }

        private MessageType(string code)
        {
            ArgumentNullException.ThrowIfNull(code, nameof(code));

            // The ToUpper is just a santiy check. F-list specifies that all codes are all-caps.
            Code = code.ToUpper();

            if (!_registry.TryAdd(Code, this))
            {
                throw new InvalidOperationException(
                    $"MessageType \"{Code}\" was already declared! Check " +
                    $"for duplicate message types in the static member " +
                    $"list for {typeof(MessageType).FullName}.");
            }
        }

        public override string ToString() => Code;

        public static MessageType? Get(string code)
        {
            if (_registry.TryGetValue(code, out var messageType))
            {
                return messageType;
            }

            return null;
        }
    }
}
