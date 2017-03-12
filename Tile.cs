using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
/*
 * Stores ui_elements data for easy access
 */
namespace beta_windows
{
    public struct tile_struct
    {
        public Texture2D tile_texture; // each Tile texture has 6 base Tile sprites
        public string name;            // lookup value, e.g. "ground"
        public short id;               // numeric value for this Tile (if Tile id is 0 - it'engine air) 65535 = max value
        public int water_volume;       // if this is a liquid tile - store water volume. 0 for solid ui_elements

        public tile_struct(Texture2D txtr, String nm, short id, int vol = 0)
        {
            tile_texture = txtr;
            name = nm;
            this.id = id;
            water_volume = vol;
        }

        public short get_id()
        {
            return id;
        }

        public String get_name()
        {
            return name;
        }

        public Texture2D get_tile_icon_clip()
        {
            Color[] imageData = new Color[tile_texture.Width * tile_texture.Height];
            tile_texture.GetData<Color>(imageData);

            Rectangle crop = new Rectangle(180, 0, 20, 20);
            Color[] imagePiece = GetImageData(imageData, tile_texture.Width, crop);
            Texture2D subtexture = new Texture2D(Game1.graphics.GraphicsDevice, crop.Width, crop.Height);
            subtexture.SetData<Color>(imagePiece);
            return subtexture;
        }

        private Color[] GetImageData(Color[] colorData, int width, Rectangle rectangle)
        {
            Color[] color = new Color[rectangle.Width * rectangle.Height];
            for (int x = 0; x < rectangle.Width; x++)
                for (int y = 0; y < rectangle.Height; y++)
                    color[x + y * rectangle.Width] = colorData[x + rectangle.X + (y + rectangle.Y) * width];
            return color;
        }
    }
    public static class Tile
    {
        public static List<tile_struct> tile_list = new List<tile_struct>();
        //public static List<water_tile> water_tile_list = new List<water_tile>();

        // register a new Tile design
        public static void add_tile(Texture2D asset, String tile_name, short id)
        {
            tile_list.Add(new tile_struct(asset, tile_name, id));
        }
        public static List<tile_struct> get_list_of_tiles()
        {
            return tile_list;
        }

        /*public static List<water_tile> get_list_of_water_tiles()
        {
            return water_tile_list;
        }*/

        /*public static void draw_water_tiles()
        {

        }*/

        public static tile_struct get_tile_struct(short id)
        {
            foreach (tile_struct t in tile_list)
            {
                if (t.get_id() == id)
                {
                    return t;
                }
            }
            return default(tile_struct);
        }
        // find a texture 2d based on the name provided
        public static Texture2D find_tile(String name)
        {
            foreach (tile_struct element in tile_list)
            {
                if (element.name == name)
                {
                    return element.tile_texture;
                }
            }
            return null;
        }
        // find a texture 2d based on the id number provided
        public static Texture2D find_tile(int index)
        {
            try
            {
                return tile_list.ElementAt(index - 1).tile_texture; // adjust for array starting at 0
            }
            catch (ArgumentOutOfRangeException)
            {
                return null;
            }
        }

        // find Tile id by name 
        public static short find_tile_id(String name)
        {
            foreach (tile_struct element in tile_list)
            {
                if (element.name == name)
                {
                    return element.id;
                }
            }
            return 0;
        }

        // find Tile name by id
        public static String find_tile_name(int id)
        {
            foreach (tile_struct element in tile_list)
            {
                if (element.id == id)
                {
                    return element.name;
                }
            }
            return "";
        }
    }
}