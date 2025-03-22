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
using Serilog;

namespace DarkestBot.UserCommands.Commands
{
    internal sealed class DiceBotGivePotionCommand(Queue<string> potionBuyers, StateManager stateManager) : IAsyncUserCommand
    {
        private const string ExpectedPotionGiver = "Dice Bot";

        public UserCommandMode AllowedModes => UserCommandMode.Public;

        public async Task TryExecuteAsync(string commandSender, string message, IChatResponder responder, CancellationToken token = default)
        {
            if (!string.Equals(commandSender, ExpectedPotionGiver, StringComparison.Ordinal))
            {
                return;
            }

            if (!PotionParser.TryParse(message, out var potion))
            {
                return;
            }

            if (potionBuyers.TryDequeue(out var potionBuyer))
            {
                Log.Information("{buyer} bought a potion!", potionBuyer);
                responder.SendChatMessage($"[user]{potionBuyer}[/user] has received: [b]{potion.Name}[/b]");
                await stateManager.ModifyAsync(state =>
                {
                    state.AddPotion(potionBuyer, potion);
                }, token);
                return;
            }

            Log.Warning("I don't know who bought a potion!");
            responder.SendChatMessage($"I don't know who bought that potion! I've lost track of the Dice Bot commands...");
        }
    }
}
