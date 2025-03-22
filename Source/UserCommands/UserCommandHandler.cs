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
using DarkestBot.Protocol;
using Serilog;
using System.Text.Json;

namespace DarkestBot.UserCommands
{
    internal sealed class UserCommandHandler(JsonSerializerOptions jsonOptions, State state, ICommandSender commandSender, UserCommandMode mode)
    {
        private const string DataDumpCommand = "!datadump";
        private const string GeneratePotionCommand = "!generatepotion";

        private const string ExpectedPotionGiver = "Dice Bot";

        private const string PotionPurchaseFailMessageStart = "Failed: You could not afford to buy a potion for";

        private readonly Queue<string> _potionBuyers = new();

        public async Task HandleCommandAsync(string character, string message, CancellationToken token = default)
        {
            if (mode == UserCommandMode.Private && message.StartsWith(DataDumpCommand, StringComparison.OrdinalIgnoreCase))
            {
                HandleDataDump(character);
                return;
            }

            if (mode == UserCommandMode.Public && message.StartsWith(GeneratePotionCommand, StringComparison.OrdinalIgnoreCase))
            {
                Log.Information("{character} wants to buy a potion!", character);
                _potionBuyers.Enqueue(character);
                return;
            }

            if (mode == UserCommandMode.Public && PotionParser.TryParse(message, out var potion))
            {
                HandlePotion(potion, character);
                return;
            }

            if (message.StartsWith(PotionPurchaseFailMessageStart, StringComparison.Ordinal))
            {
                HandlePotionPurchaseFail(character);
                return;
            }
        }

        private void HandleDataDump(string character)
        {
            if (state.Characters.TryGetValue(character, out var data))
            {
                var dump = JsonSerializer.Serialize(data, jsonOptions);
                commandSender.SendCommand(CommandFactory.ChannelMessage(state.RoomId, dump));
            }
            else
            {
                commandSender.SendCommand(CommandFactory.ChannelMessage(state.RoomId, $"No data found for [user]{character}[/user]"));
            }
        }

        private void HandlePotion(PotionParser.ParsedPotion potion, string potionGiver)
        {
            if (!string.Equals(potionGiver, ExpectedPotionGiver, StringComparison.Ordinal))
            {
                commandSender.SendCommand(CommandFactory.ChannelMessage(state.RoomId, $"Nice try, [user]{potionGiver}[/user]!"));
                return;
            }

            if (_potionBuyers.TryDequeue(out var potionBuyer))
            {
                Log.Information("{buyer} bought a potion!");
                commandSender.SendCommand(CommandFactory.ChannelMessage(state.RoomId, $"[user]{potionBuyer}[/user] has received: [b]{potion.Name}[/b]"));
                return;
            }

            Log.Warning("I don't know who bought a potion!");
            commandSender.SendCommand(CommandFactory.ChannelMessage(state.RoomId, $"I don't know who bought that potion! I've lost track of the Dice Bot commands..."));
        }

        private void HandlePotionPurchaseFail(string? character)
        {
            // if the character is null or NOT the dice bot (potion giver), return.
            if (character == null || !character.Equals(ExpectedPotionGiver, StringComparison.Ordinal))
            {
                return;
            }

            if (_potionBuyers.TryDequeue(out var potionBuyer))
            {
                Log.Information("{character} cannot afford a potion!", potionBuyer);
            }
            else
            {
                Log.Warning("I don't know who can't afford a potion!");
            }
        }
    }
}
