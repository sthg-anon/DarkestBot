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

using Serilog;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace DarkestBot.Protocol
{
    internal static class PayloadParser
    {
        public static bool TryParsePayload<T>(JsonSerializerOptions jsonOptions, string? input, [NotNullWhen(true)] out T? payload) where T : class
        {
            payload = null;

            if (input == null)
            {
                Log.Error("{name} payload is empty.", typeof(T).Name);
                return false;
            }

            try
            {
                payload = JsonSerializer.Deserialize<T>(input, jsonOptions);
            }
            catch (JsonException ex)
            {
                Log.Error(ex, "Unable to parse {name} payload.", typeof(T).Name);
                return false;
            }

            if (payload == null)
            {
                Log.Error("{name} payload parsed to null.", typeof(T).Name);
                return false;
            }

            return true;
        }
    }
}
