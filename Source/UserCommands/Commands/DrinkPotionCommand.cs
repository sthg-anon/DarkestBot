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

namespace DarkestBot.UserCommands.Commands
{
    internal sealed class DrinkPotionCommand(StateManager stateManager) : IAsyncUserCommand
    {
        private const string CommandPrefix = "!drinkpotion";

        public UserCommandMode AllowedModes => UserCommandMode.Public;

        public async Task TryExecuteAsync(string commandSender, string message, IChatResponder responder, CancellationToken token = default)
        {
            if (!message.Equals(CommandPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            string[] parts = message.Split(' ');
            if (parts.Length < 2)
            {
                responder.SendChatMessage("You need to specify a potion name! Use !listpotions to see what potions you have, then drink it with !drinkpotion [b]<name>[/b].");
                return;
            }

            Potion? potion = null;
            await stateManager.ModifyAsync(s =>
            {
                potion = s.RemovePotion(commandSender, parts[1]);
            }, token);

            if (potion == null)
            {
                responder.SendChatMessage($"[user]{commandSender}[/user] does not have that potion.");
                return;
            }

            responder.SendChatMessage($"[user]{commandSender}[/user] drinks their [eicon]{potion.Eicon}[/eicon] [b]{potion.Name}[/b] potion!");
        }
    }
}
