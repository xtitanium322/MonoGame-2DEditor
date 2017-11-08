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

namespace EditorEngine
{
    /// <summary>
    /// Tile structure - contains tile definitions
    /// </summary>
    public struct tile_struct
    {
        public Texture2D tile_texture; // each Tile texture has 6 base Tile sprites
        public string name;            // lookup value, e.g. "ground"
        public short id;               // numeric value for this Tile (if Tile id is 0 - it'engine air) 65535 = max value
        public int water_volume;       // if this is a liquid tile - store water volume. 0 for solid ui_elements

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="txtr">Texture for this tile type</param>
        /// <param name="nm">tile name</param>
        /// <param name="id">tile id number</param>
        /// <param name="vol">tile water volume</param>
        public tile_struct(Texture2D txtr, String nm, short id, int vol = 0)
        {
            tile_texture = txtr;
            name = nm;
            this.id = id;
            water_volume = vol;
        }
        /// <summary>
        /// get tile string id
        /// </summary>
        /// <returns>tile id</returns>
        public short get_id()
        {
            return id;
        }
        /// <summary>
        /// Get tile name
        /// </summary>
        /// <returns>string representation fo tile name</returns>
        public String get_name()
        {
            return name;
        }
        /// <summary>
        /// Tile clip - from the sprite sheet
        /// </summary>
        /// <returns>Texture2d if the tile - the icon portion</returns>
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
        /// <summary>
        /// Convert tile clip to color array
        /// </summary>
        /// <param name="colorData">Color array</param>
        /// <param name="width">width of the image</param>
        /// <param name="rectangle">clip rectangle</param>
        /// <returns>Color array</returns>
        private Color[] GetImageData(Color[] colorData, int width, Rectangle rectangle)
        {
            Color[] color = new Color[rectangle.Width * rectangle.Height];
            for (int x = 0; x < rectangle.Width; x++)
                for (int y = 0; y < rectangle.Height; y++)
                    color[x + y * rectangle.Width] = colorData[x + rectangle.X + (y + rectangle.Y) * width];
            return color;
        }
    }
    /// <summary>
    /// Tile contains a list of tiles defined above
    /// It is not added to world_map but used to handle tile structs for easy lookup
    /// </summary>
    public static class Tile
    {
        public static List<tile_struct> tile_list = new List<tile_struct>();

        /// <summary>
        /// Add a new tile type
        /// </summary>
        /// <param name="asset">Texture spritesheet</param>
        /// <param name="tile_name">tile name</param>
        /// <param name="id">tile id</param>
        public static void add_tile(Texture2D asset, String tile_name, short id)
        {
            tile_list.Add(new tile_struct(asset, tile_name, id));
        }
        /// <summary>
        /// List of tile structs
        /// </summary>
        /// <returns>a list of tiles</returns>
        public static List<tile_struct> get_list_of_tiles()
        {
            return tile_list;
        }
        /// <summary>
        /// get tile struct representation of the tile
        /// </summary>
        /// <param name="id">tile numeric id</param>
        /// <returns>tile struct</returns>
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
        /// <summary>
        /// find a texture 2d based on the name provided
        /// </summary>
        /// <param name="name">string id representation</param>
        /// <returns>Texture of the tile</returns>
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
        /// <summary>
        /// find a texture 2d based on the id number provided
        /// </summary>
        /// <param name="index">numeric id</param>
        /// <returns>Texture of the tile</returns>
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

        /// <summary>
        ///  find Tile id by name 
        /// </summary>
        /// <param name="name">string name</param>
        /// <returns>tile id - numeric</returns>
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

        /// <summary>
        /// find Tile name by id
        /// </summary>
        /// <param name="id">numeric id</param>
        /// <returns>string name</returns>
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