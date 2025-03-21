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
using Serilog;

namespace DarkestBot.Model
{
    internal static class StateExtensions
    {
        public static void AddPotion(this State state, string characterName, Potion potion)
        {
            if (potion.Name == null || potion.Eicon == null || potion.Description == null)
            {
                Log.Error("Potion is missing data!");
                return;
            }

            if (!state.Characters.TryGetValue(characterName, out var character))
            {
                character = new Character();
                state.Characters.Add(characterName, character);
            }

            character.Potions ??= [];
            character.Potions.Add(potion);
        }

        public static Potion? RemovePotion(this State state, string characterName, string potionName)
        {
            if (!state.Characters.TryGetValue(characterName, out var character))
            {
                return null;
            }

            if (character.Potions == null)
            {
                return null;
            }

            var potion = character.Potions.Find(p => p.Name?.Equals(potionName, StringComparison.InvariantCultureIgnoreCase) ?? false);
            if (potion == null)
            {
                return null;
            }

            character.Potions.Remove(potion);
            return potion;
        }
    }
}
