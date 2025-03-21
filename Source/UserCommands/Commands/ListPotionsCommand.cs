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
using Serilog;
using System.Text;

namespace DarkestBot.UserCommands.Commands
{
    internal class ListPotionsCommand(StateManager stateManager) : IUserCommand
    {
        private const string CommandPrefix = "!listpotions";

        public UserCommandMode AllowedModes => UserCommandMode.Public | UserCommandMode.Private;

        public void TryExecute(string commandSender, string message, IChatResponder responder)
        {
            if (!message.Equals(CommandPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (!stateManager.State.Characters.TryGetValue(commandSender, out var character))
            {
                responder.SendChatMessage($"[user]{commandSender}[/user] has no potions.");
                return;
            }

            var potions = character.Potions;
            if (potions == null ||  potions.Count == 0 )
            {
                responder.SendChatMessage($"[user]{commandSender}[/user] has no potions.");
                return;
            }

            var sb = new StringBuilder();
            sb.Append("[user]").Append(commandSender).Append("[/user]'s potions:\n");

            foreach (var potion in potions)
            {
                if (potion.Name == null || potion.Eicon == null || potion.Description == null)
                {
                    Log.Warning("{character}'s potion has missing data!", commandSender);
                }

                sb.Append("[eicon]").Append(potion.Eicon).Append("[/eicon] [b]").Append(potion.Name).Append("[/b]: ").Append(potion.Description).Append('\n');
            }

            responder.SendChatMessage(sb.ToString().TrimEnd('\n'));
        }
    }
}
