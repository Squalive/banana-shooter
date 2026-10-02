using System.Collections.Generic;
using UnityEngine;

namespace Cosmetic
{
    public class CosmeticManager
    {
        public static Dictionary<int, CosmeticItem> ItemIdToItem = new Dictionary<int, CosmeticItem>();

        public static Dictionary<string, ushort> ColorIndexes = new()
        {
            {"light blue",0},{"white",1},{"red",2},
            {"yellow",3}, {"grey",4}, {"black",5},  {"blue",6}, {"green",7} , 
            {"orange",8}, {"brown",9}, {"golden",10}, 
            {"mediumslateblue",11}, {"pink", 12}, {"purple",13},
            {"ruby", 14}, {"emerald",15}, {"sapphire", 16}
        };

        // public static Dictionary<string, Color> Colors = new() { {"light blue",Color.cyan},{"white",Color.white},{"red",Color.red},
        //     {"yellow",Color.yellow}, {"grey",Color.grey}, {"black",Color.black},  {"blue",new Color(30/255f,144/255f,255/255f)}, {"green",new Color(0,255/255f,0)} , 
        //     {"orange",new Color(255/255f,165/255f,0)}, {"brown",new Color(	165/255f,42/255f,42/255f)}, {"golden",new Color(		255/255f,215/255f,0/255f)}, 
        //     {"mediumslateblue",new Color(123f/255f, 104f/255, 238f/255f)}, {"pink", new Color(255/255f, 182/255f, 193/255f)}, {"purple",new Color(139 / 255f, 0 / 255f, 139 / 255f)},
        //     {"ruby", new Color(232/255f,0,0)}, {"emerald",new Color(71/255f,1f,0) }, {"sapphire", new Color(0,128/255f,254/255f)}
        // };

        public static Color[] Colors = new[]
        {
            Color.cyan, Color.white, Color.red, Color.yellow, Color.grey, Color.black,
            new Color(30 / 255f, 144 / 255f, 255 / 255f), new Color(0, 255 / 255f, 0),
            new Color(255 / 255f, 165 / 255f, 0), new Color(165 / 255f, 42 / 255f, 42 / 255f),
            new Color(255 / 255f, 215 / 255f, 0 / 255f), new Color(123f / 255f, 104f / 255, 238f / 255f),
            new Color(255 / 255f, 182 / 255f, 193 / 255f), new Color(139 / 255f, 0 / 255f, 139 / 255f),
            new Color(232 / 255f, 0, 0), new Color(71 / 255f, 1f, 0), new Color(0, 128 / 255f, 254 / 255f)
        };

        public static Color GetColor(string n)
        {
            if (ColorIndexes.TryGetValue(n, out var color))
            {
                return Colors[color];
            }
            return Color.clear;
        }

        public static bool IsGoldenColor(Color color)
        {
            return color == GetColor("golden") || color == GetColor("ruby") || color == GetColor("emerald") ||
                   color == GetColor("sapphire");
        }
    }
}
