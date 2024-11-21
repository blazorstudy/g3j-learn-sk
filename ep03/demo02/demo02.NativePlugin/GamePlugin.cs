using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.SemanticKernel;

namespace demo02.NativePlugin
{
    public class GamePlugin
    {
        [KernelFunction("roll_dice")]
        [Description("get the number from 1 to 6 ")]
        [return: Description("number from 1 to 6")]
        public int RollDice()
        {
            return Random.Shared.Next(1, 7);
        }

        [KernelFunction("flip_coin")]
        [Description("get the result of a coin flip")]
        [return: Description("Heads or Tails")]
        public string FlipCoin()
        {
            return Random.Shared.Next(0, 2) == 0 ? "Heads" : "Tails";
        }
    }
}
